using Minimatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;

namespace EditorConfig
{
    partial class EditorConfigValidator : IDisposable
    {
        private const int _validationDelay = 500;
        private static string[] _ignorePaths = { "\\node_modules", "\\.git", "\\packages", "\\bower_components", "\\jspm_packages", "\\testresults", "\\.vs" };

        private EditorConfigDocument _document;
        private DateTime _lastRequestForValidation;
        private Timer _timer;
        private bool _hasChanged;
        private bool _prevEnabled = EditorConfigPackage.ValidationOptions != null ? EditorConfigPackage.ValidationOptions.EnableValidation : true;
        private Dictionary<string, bool> _globbingCache = new Dictionary<string, bool>();
        private static readonly Options _options = new Options { AllowWindowsPaths = true, MatchBase = true };

        private EditorConfigValidator(EditorConfigDocument document)
        {
            _document = document;
            _document.Parsed += DocumentParsedAsync;

            if (_prevEnabled)
                ValidateAsync().ConfigureAwait(false);

            ValidationOptions.Saved += DocumentParsedAsync;
        }

        public bool IsValidating { get; private set; }

        /// <summary>Gets or creates an instace of the validator and stores it in the text buffer properties.</summary>
        public static EditorConfigValidator FromDocument(EditorConfigDocument document)
        {
            return document.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigValidator(document));
        }

        private async void DocumentParsedAsync(object sender, EventArgs e)
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
                await RequestValidationAsync(false);
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
                    _timer.Elapsed += TimerElapsedAsync;
                }

                _hasChanged = true;
                _timer.Enabled = true;
            }
        }

        private async void TimerElapsedAsync(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.AddMilliseconds(-_validationDelay) > _lastRequestForValidation && _hasChanged && !_document.IsParsing)
            {
                _timer.Stop();
                await ValidateAsync();
            }
        }

        private void ClearAllErrors()
        {
            foreach (ParseItem item in _document.ParseItems.Where(i => i.Errors.Any()))
            {
                item.Errors.Clear();
            }
        }

        private async Task ValidateAsync()
        {
            if (IsValidating) return;

            IsValidating = true;

            await Task.Run(() =>
            {
                try
                {
                    ValidateUnknown();
                    ValidateRootProperties();
                    ValidateSections();

                    Validated?.Invoke(this, EventArgs.Empty);
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
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            Validated = null;

            _document.Parsed -= DocumentParsedAsync;
            ValidationOptions.Saved -= DocumentParsedAsync;
        }

        public event EventHandler Validated;
    }
}
