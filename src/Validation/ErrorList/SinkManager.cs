using Microsoft.VisualStudio.Shell.TableManager;
using System.Linq;
using System;
using System.Collections.Generic;

namespace EditorConfig
{
    class SinkManager : IDisposable
    {
        private readonly ITableDataSink _sink;
        private readonly TableDataSource _errorList;
        private readonly List<TableEntriesSnapshot> _snapshots = [];

        internal SinkManager(TableDataSource errorList, ITableDataSink sink)
        {
            _sink = sink;
            _errorList = errorList;

            errorList.AddSinkManager(this);
        }

        internal void Clear()
        {
            _snapshots.Clear();
            _sink.RemoveAllSnapshots();
        }

        internal void UpdateSink(IEnumerable<TableEntriesSnapshot> snapshots)
        {
            foreach (TableEntriesSnapshot snapshot in snapshots)
            {
                TableEntriesSnapshot existing = _snapshots.FirstOrDefault(s => string.Equals(s.Url, snapshot.Url, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    _snapshots.Remove(existing);
                    _sink.ReplaceSnapshot(existing, snapshot);
                }
                else
                {
                    _sink.AddSnapshot(snapshot);
                }

                _snapshots.Add(snapshot);
            }
        }

        internal void RemoveSnapshots(IEnumerable<string> urls)
        {
            foreach (string url in urls)
            {
                TableEntriesSnapshot existing = _snapshots.FirstOrDefault(s => string.Equals(s.Url, url, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    _snapshots.Remove(existing);
                    _sink.RemoveSnapshot(existing);
                }
            }
        }

        public void Dispose()
        {
            // Called when the person who subscribed to the data source disposes of the cookie (== this object) they were given.
            _errorList.RemoveSinkManager(this);
        }
    }
}
