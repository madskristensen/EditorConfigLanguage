using System;
using System.Collections.Generic;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorConfig
{
    internal sealed class FindNamingReferences(IWpfTextView view) : BaseCommand
    {
        private static readonly Guid _commandGroup = typeof(VSConstants.VSStd97CmdID).GUID;
        private const uint _commandId = (uint)VSConstants.VSStd97CmdID.FindReferences;

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup != _commandGroup || nCmdID != _commandId ||
                !TryGetReferences(out NamingSymbol symbol, out IReadOnlyList<NamingReferenceResult> references))
            {
                return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            var service = ServiceProvider.GlobalProvider.GetService(typeof(SVsFindAllReferences)) as IFindAllReferencesService;
            if (service == null)
                return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            IFindAllReferencesWindow window = service.StartSearch($"'{symbol.Name}' references");
            var source = new NamingReferencesDataSource(symbol, references, EditorConfigDocument.FromTextBuffer(view.TextBuffer).FileName);
            window.Manager.AddSource(source, NamingReferencesDataSource.Columns);
            window.SetProgress(references.Count, references.Count);

            EventHandler closed = null;
            closed = (_, _) =>
            {
                window.Closed -= closed;
                window.Manager.RemoveSource(source);
            };
            window.Closed += closed;

            return VSConstants.S_OK;
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == _commandGroup && prgCmds[0].cmdID == _commandId && TryGetReferences(out _, out _))
            {
                prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                return VSConstants.S_OK;
            }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        private bool TryGetReferences(out NamingSymbol symbol, out IReadOnlyList<NamingReferenceResult> references)
        {
            EditorConfigDocument document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);
            int position = view.Caret.Position.BufferPosition.Position;

            if (NamingSymbolService.TryGetSymbolAtPosition(document, position, out symbol))
            {
                references = NamingSymbolService.FindReferenceResults(
                    document,
                    view.TextSnapshot,
                    symbol.Kind,
                    symbol.Name);
                return references.Count > 0;
            }

            references = [];
            return false;
        }
    }

    internal sealed class NamingReferencesDataSource : ITableDataSource
    {
        internal static readonly string[] Columns =
        [
            StandardTableColumnDefinitions2.Definition,
            StandardTableColumnDefinitions2.LineText,
            StandardTableColumnDefinitions.DocumentName,
            StandardTableColumnDefinitions.Line,
            StandardTableColumnDefinitions.Column,
        ];

        private readonly NamingReferencesSnapshot _snapshot;

        internal NamingReferencesDataSource(
            NamingSymbol symbol,
            IReadOnlyList<NamingReferenceResult> references,
            string fileName)
        {
            _snapshot = new NamingReferencesSnapshot(symbol, references, fileName);
        }

        public string SourceTypeIdentifier => StandardTableDataSources.FindAddReferencesDataSource;

        public string Identifier => PackageGuids.guidEditorConfigPackageString;

        public string DisplayName => Vsix.Name;

        public IDisposable Subscribe(ITableDataSink sink)
        {
            sink.AddSnapshot(_snapshot);
            return new NamingReferencesSubscription(sink, _snapshot);
        }

        private sealed class NamingReferencesSubscription : IDisposable
        {
            private ITableDataSink _sink;
            private readonly NamingReferencesSnapshot _snapshot;

            internal NamingReferencesSubscription(ITableDataSink sink, NamingReferencesSnapshot snapshot)
            {
                _sink = sink;
                _snapshot = snapshot;
            }

            public void Dispose()
            {
                _sink?.RemoveSnapshot(_snapshot);
                _sink = null;
            }
        }
    }

    internal sealed class NamingReferencesSnapshot : TableEntriesSnapshotBase
    {
        private readonly NamingSymbol _symbol;
        private readonly IReadOnlyList<NamingReferenceResult> _references;
        private readonly string _fileName;

        internal NamingReferencesSnapshot(
            NamingSymbol symbol,
            IReadOnlyList<NamingReferenceResult> references,
            string fileName)
        {
            _symbol = symbol;
            _references = references;
            _fileName = fileName;
        }

        public override int VersionNumber => 1;

        public override int Count => _references.Count;

        public override bool TryGetValue(int index, string columnName, out object content)
        {
            if (index < 0 || index >= Count)
            {
                content = null;
                return false;
            }

            NamingReferenceResult reference = _references[index];

            switch (columnName)
            {
                case StandardTableKeyNames.Definition:
                    content = $"{_symbol.Kind}: {_symbol.Name}";
                    return true;
                case StandardTableKeyNames.LineText:
                case StandardTableKeyNames.Text:
                    content = reference.LineText;
                    return true;
                case StandardTableKeyNames.DocumentName:
                    content = _fileName;
                    return true;
                case StandardTableKeyNames.Line:
                    content = reference.Line;
                    return true;
                case StandardTableKeyNames.Column:
                    content = reference.Column;
                    return true;
                default:
                    content = null;
                    return false;
            }
        }
    }
}
