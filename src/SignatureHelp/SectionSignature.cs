﻿using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EditorConfig
{
    public class SectionSignature : ISignature
    {
        private string _propertyName;
        private string _syntax;
        private string _description;
        private string _content;
        private GenericParameter _nameParam;
        private IParameter _currentParam;
        private ITrackingSpan _trackingSpan;
        private ISignatureHelpSession _session;

        public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

        public SectionSignature(
            string syntax,
            string description,
            ITrackingSpan trackingSpan,
            ISignatureHelpSession session)
        {

            _propertyName = "Example";
            _syntax = syntax ?? string.Empty;
            _description = description;
            _trackingSpan = trackingSpan;

            _content = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", _propertyName, _syntax);
            _nameParam = new GenericParameter(this);
            _currentParam = _nameParam;

            _session = session;

            // In order to dismiss this tip at the appropriate time, I need to listen
            // to changes in the text buffer
            if (_trackingSpan != null && _session != null)
            {
                _session.Dismissed += OnSessionDismissed;
                _trackingSpan.TextBuffer.Changed += OnTextBufferChanged;
            }
        }

        public ITrackingSpan ApplicableToSpan
        {
            get { return _trackingSpan; }
        }

        public string PropertyName
        {
            get { return _propertyName; }
        }

        public string Content
        {
            get { return _content; }
        }

        public IParameter CurrentParameter
        {
            get { return _nameParam; }

            set
            {
                if (value != _currentParam)
                {
                    IParameter oldParam = _currentParam;
                    _currentParam = value;

                    CurrentParameterChanged?.Invoke(this, new CurrentParameterChangedEventArgs(oldParam, _currentParam));
                }
            }
        }

        public string Documentation
        {
            get { return _description; }
        }

        public ReadOnlyCollection<IParameter> Parameters
        {
            get
            {
                var parameters = new List<IParameter> { _nameParam };
                return new ReadOnlyCollection<IParameter>(parameters);
            }
        }

        /// <summary>
        /// This is called when there isn't enough room on the screen to show the normal content
        /// </summary>
        public string PrettyPrintedContent
        {
            get { return Content; }
        }

        /// <summary>
        /// I'm about to be destroyed, so stop listening to events
        /// </summary>
        private void OnSessionDismissed(object sender, System.EventArgs eventArgs)
        {
            if (_trackingSpan != null)
            {
                _trackingSpan.TextBuffer.Changed -= OnTextBufferChanged;
            }

            if (_session != null)
            {
                _session.Dismissed -= OnSessionDismissed;
                _session = null;
            }
        }

        /// <summary>
        /// Check if the property name in the text buffer has changed.
        /// If so, then dismiss the syntax help tip.
        /// </summary>
        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs eventArgs)
        {
            if (_trackingSpan != null && _session != null)
            {
                ITextSnapshot snapshot = _trackingSpan.TextBuffer.CurrentSnapshot;
                SnapshotPoint startPoint = _trackingSpan.GetStartPoint(snapshot);
                bool propertyNameStillValid = false;

                if (startPoint.Position + _propertyName.Length <= snapshot.Length)
                {
                    string text = _trackingSpan.GetText(snapshot);

                    if (text.StartsWith("[", StringComparison.Ordinal))
                    {
                        // The correct property name is still in the code
                        propertyNameStillValid = true;
                        _session.Match();
                    }
                }

                if (!propertyNameStillValid)
                {
                    _session.Dismiss();
                }
            }
        }
    }

}
