using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

using Microsoft.VisualStudio.Text;

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
        private DateTime _lastRequestForValidation;
        private Timer _timer;
        private bool _hasChanged;
        private bool _prevEnabled = EditorConfigPackage.ValidationOptions == null || EditorConfigPackage.ValidationOptions.EnableValidation;
        private readonly Dictionary<string, bool> _globbingCache = [];

        private EditorConfigValidator(EditorConfigDocument document)
        {
            _document = document;
            _document.Parsed += DocumentParsed;

            if (_prevEnabled)
                _ = ValidateAsync();

            ValidationOptions.Saved += DocumentParsed;
        }

        public bool IsValidating { get; private set; }

        /// <summary>Gets or creates an instace of the validator and stores it in the text buffer properties.</summary>
        public static EditorConfigValidator FromDocument(EditorConfigDocument document)
        {
            return document.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigValidator(document));
        }

        private void DocumentParsed(object sender, EventArgs e)
        {
            if (!EditorConfigPackage.ValidationOptions.EnableValidation)
            {
                // Don't run the logic unless the user changed the settings since last run
                if (_prevEnabled != EditorConfigPackage.ValidationOptions.EnableValidation)
                {
                    ClearAllErrors();
                    Validated?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _ = RequestValidationAsync(false);
            }

            _prevEnabled = EditorConfigPackage.ValidationOptions.EnableValidation;
        }

        /// <summary>Schedules an async validation run.</summary>
        public async Task RequestValidationAsync(bool force)
        {
            _lastRequestForValidation = DateTime.Now;

            if (force)
            {
                _globbingCache.Clear();
                ClearAllErrors();
                await ValidateAsync();
            }
            else
            {
                if (_timer == null)
                {
                    _timer = new Timer(_validationDelay);
                    _timer.Elapsed += TimerElapsed;
                }

                _hasChanged = true;
                _timer.Enabled = true;
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.AddMilliseconds(-_validationDelay) > _lastRequestForValidation && _hasChanged && !_document.IsParsing)
            {
                _timer.Stop();
                _ = ValidateAsync();
            }
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
            if (IsValidating || _document.IsParsing) return;

            IsValidating = true;

            await Task.Run(() =>
            {
                try
                {
                    ValidateUnknown();
                    ValidateRootProperties();
                    ValidateSections();
                }
                catch (Exception ex)
                {
                    Telemetry.TrackException("Validate", ex);
                }
                finally
                {
                    _hasChanged = false;
                    IsValidating = false;
                }
            });

            Validated?.Invoke(this, EventArgs.Empty);
        }

        public void SuppressError(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode) || _document.Suppressions.Contains(errorCode))
                return;

            var range = new Span(0, 0);
            IEnumerable<string> errorCodes = _document.Suppressions.Union([errorCode]).OrderBy(c => c);

            if (_document.Suppressions.Any())
            {
                int position = _document.ParseItems.First().Span.Start;
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
