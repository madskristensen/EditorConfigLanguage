using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace EditorConfig
{
    class TableDataSource : ITableDataSource
    {
        private static TableDataSource _instance;
        private readonly List<SinkManager> _managers = [];
        private readonly Dictionary<string, TableEntriesSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);

        [Import]
        private ITableManagerProvider TableManagerProvider { get; set; } = null;

        private TableDataSource()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(compositionService);
            compositionService.DefaultCompositionService.SatisfyImportsOnce(this);

            ITableManager manager = TableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            manager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName,
                                                   StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column);
        }

        public static TableDataSource Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TableDataSource();

                return _instance;
            }
        }

        public bool HasErrors
        {
            get
            {
                lock (_managers)
                    return _snapshots.Values.Any(snapshot => snapshot.Count > 0);
            }
        }

        #region ITableDataSource members
        public string SourceTypeIdentifier
        {
            get { return StandardTableDataSources.ErrorTableDataSource; }
        }

        public string Identifier
        {
            get { return PackageGuids.guidEditorConfigPackageString; }
        }

        public string DisplayName
        {
            get { return Vsix.Name; }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            return new SinkManager(this, sink);
        }
        #endregion

        public void AddSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Add(manager);
                manager.UpdateSink(_snapshots.Values.ToArray());
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Remove(manager);
            }
        }

        public void UpdateAllSinks()
        {
            lock (_managers)
            {
                foreach (SinkManager manager in _managers.ToArray())
                {
                    manager.UpdateSink(_snapshots.Values.ToArray());
                }
            }
        }

        public void AddErrors(IEnumerable<ParseItem> result, string projectName, string fileName)
        {
            var snapshot = new TableEntriesSnapshot(result, projectName, fileName);
            lock (_managers)
            {
                _snapshots.TryGetValue(fileName, out TableEntriesSnapshot previous);
                _snapshots[fileName] = snapshot;
                UpdateAllSinks();
                previous?.Dispose();
            }
        }

        public void CleanErrors(params string[] urls)
        {
            lock (_managers)
            {
                foreach (string url in urls)
                {
                    if (_snapshots.TryGetValue(url, out TableEntriesSnapshot snapshot))
                    {
                        _snapshots.Remove(url);
                        snapshot.Dispose();
                    }
                }

                foreach (SinkManager manager in _managers.ToArray())
                {
                    manager.RemoveSnapshots(urls);
                }
                UpdateAllSinks();
            }
        }

        public void CleanAllErrors()
        {
            lock (_managers)
            {
                foreach (TableEntriesSnapshot snapshot in _snapshots.Values)
                    snapshot.Dispose();

                _snapshots.Clear();
                foreach (SinkManager manager in _managers.ToArray())
                {
                    manager.Clear();
                }
                UpdateAllSinks();
            }
        }
    }
}
