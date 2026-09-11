using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Microsoft.VisualStudio.Text;

using Timer = System.Timers.Timer;

namespace EditorConfig
{
    partial class EditorConfigValidator : IDisposable
    {
        private const int _validationDelay = 500;
        private const int _maxRecursionDepth = 5; // Limit directory traversal depth

        // Use HashSet for O(1) lookup instead of array with LINQ Any()
        private static readonly HashSet<string> _ignorePaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "\\node_modules", "\\.git", "\\packages", "\\bower_components",
            "\\jspm_packages", "\\testresults", "\\.vs", "\\bin", "\\obj"
        };

        private readonly EditorConfigDocument _document;
        private Timer _timer;
        private bool _prevEnabled = EnableValidation;
        private readonly Dictionary<string, bool> _globbingCache = [];
        private readonly SemaphoreSlim _validationGate = new(1, 1);
        private int _latestValidationRequestId;
        private int _validatedRequestId;
        private int _isValidating;

        private EditorConfigValidator(EditorConfigDocument document)
        {
            _document = document;
            _document.Parsed += DocumentParsed;

            if (_prevEnabled)
            {
                Interlocked.Increment(ref _latestValidationRequestId);
                _ = ValidateAsync();
            }

            ValidationOptions.Saved += DocumentParsed;
        }

        public bool IsValidating => Volatile.Read(ref _isValidating) != 0;

        internal static bool EnableValidation => EditorConfigPackage.ValidationOptions?.EnableValidation ?? true;

        internal static bool EnableUnknownProperties => EditorConfigPackage.ValidationOptions?.EnableUnknownProperties ?? true;

        internal static bool EnableUnknownValues => EditorConfigPackage.ValidationOptions?.EnableUnknownValues ?? true;

        internal static bool EnableDuplicateSections => EditorConfigPackage.ValidationOptions?.EnableDuplicateSections ?? true;

        internal static bool EnableDuplicateProperties => EditorConfigPackage.ValidationOptions?.EnableDuplicateProperties ?? true;

        internal static bool EnableDuplicateFoundInParent => EditorConfigPackage.ValidationOptions?.EnableDuplicateFoundInParent ?? true;

        internal static bool EnableGlobbingMatcher => EditorConfigPackage.ValidationOptions?.EnableGlobbingMatcher ?? true;

        internal static bool AllowSpacesInSections => EditorConfigPackage.ValidationOptions?.AllowSpacesInSections ?? false;

        internal static bool HasIgnoredPrefix(string keyword)
            => EditorConfigPackage.ValidationOptions?.HasIgnoredPrefix(keyword)
               ?? ValidationOptions.HasIgnoredPrefix(keyword, ValidationOptions.DefaultIgnoredPrefixes);

        /// <summary>Gets or creates an instace of the validator and stores it in the text buffer properties.</summary>
        public static EditorConfigValidator FromDocument(EditorConfigDocument document)
        {
            return document.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigValidator(document));
        }

        private void DocumentParsed(object sender, EventArgs e)
        {
            if (!EnableValidation)
            {
                // Don't run the logic unless the user changed the settings since last run
                if (_prevEnabled != EnableValidation)
                {
                    ClearAllErrors();
                    Validated?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _ = RequestValidationAsync(false);
            }

            _prevEnabled = EnableValidation;
        }

        /// <summary>Schedules an async validation run.</summary>
        public async Task RequestValidationAsync(bool force)
        {
            Interlocked.Increment(ref _latestValidationRequestId);

            if (force)
            {
                _globbingCache.Clear();
                await ValidateAsync();
            }
            else
            {
                if (_timer == null)
                {
                    _timer = new Timer(_validationDelay);
                    _timer.AutoReset = false;
                    _timer.Elapsed += TimerElapsed;
                }

                _timer.Stop();
                _timer.Start();
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            _ = ValidateAsync();
        }

        private void ClearAllErrors()
        {
            foreach (ParseItem item in _document.ParseItems)
            {
                if (item.Errors.Count > 0)
                {
                    item.Errors.Clear();
                }
            }
        }

        private async Task ValidateAsync()
        {
            await _validationGate.WaitAsync();
            try
            {
                while (_validatedRequestId < Volatile.Read(ref _latestValidationRequestId))
                {
                    while (_document.IsParsing)
                        await _document.ParsingTask;

                    int validationRequestId = Volatile.Read(ref _latestValidationRequestId);
                    Interlocked.Exchange(ref _isValidating, 1);

                    await Task.Run(() =>
                    {
                        try
                        {
                            ClearAllErrors();
                            ValidateUnknown();
                            ValidateRootProperties();
                            ValidateSections();
                        }
                        catch (Exception ex)
                        {
                            Telemetry.TrackException("Validate", ex);
                        }
                    });

                    _validatedRequestId = validationRequestId;
                    Validated?.Invoke(this, EventArgs.Empty);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isValidating, 0);
                _validationGate.Release();
            }
        }

        public void SuppressError(string errorCode)
        {
            // HashSet.Contains is O(1)
            if (string.IsNullOrEmpty(errorCode) || _document.Suppressions.Contains(errorCode))
                return;

            var range = new Span(0, 0);
            IEnumerable<string> errorCodes = _document.Suppressions.Union([errorCode]).OrderBy(c => c);

            if (_document.Suppressions.Count > 0)
            {
                int position = _document.ParseItems[0].Span.Start;
                ITextSnapshotLine line = _document.TextBuffer.CurrentSnapshot.GetLineFromPosition(position);
                range = Span.FromBounds(line.Start, line.EndIncludingLineBreak);
            }

            string text = string.Format("# Suppress: {0}", string.Join(" ", errorCodes)) + Environment.NewLine;

            using (ITextEdit edit = _document.TextBuffer.CreateEdit())
            {
                edit.Replace(range, text);
                edit.Apply();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;

            Validated = null;

            _document.Parsed -= DocumentParsed;
            ValidationOptions.Saved -= DocumentParsed;
        }

        public event EventHandler Validated;
    }
}
