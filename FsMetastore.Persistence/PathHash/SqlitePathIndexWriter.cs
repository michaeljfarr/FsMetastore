using System.Collections.Generic;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.Sqlite;

namespace FsMetastore.Persistence.PathHash
{
    class SqlitePathIndexWriter : SqlitePathIndexBase, IPathIndexWriter
    {

        List<KeyValuePair<ulong, long>> _fileBatch = new List<KeyValuePair<ulong, long>>();
        List<KeyValuePair<ulong, long>> _folderBatch = new List<KeyValuePair<ulong, long>>();
        private const int MinBatch = 1000;
        public SqlitePathIndexWriter(BatchIOConfig batchIOConfig) : base(batchIOConfig)
        {
        }

        protected override bool ShouldDeleteOnClose()
        {
            return false;
        }

        public override void Flush()
        {
            Flush(0);
        }

        public void Clean()
        {
            OpenConnection();
            CleanTable(FolderPositionDataTableName);
            CleanTable(MetaPositionDataTableName);
        }

        private void Flush(int minSize)
        {
            if (_folderBatch.Count > 0 && _folderBatch.Count > minSize)
            {
                WritePathIndex(_folderBatch, FolderPositionDataTableName);
                _folderBatch.Clear();
            }

            if (_fileBatch.Count > 0 && _fileBatch.Count > minSize)
            {
                WritePathIndex(_fileBatch, MetaPositionDataTableName);
                _fileBatch.Clear();
            }
        }

        private void WritePathIndex(IEnumerable<KeyValuePair<ulong, long>> lastBatch, string tableName)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                using (var command = _connection.CreateCommand())
                {

                    foreach (var item in lastBatch)
                    {
                        command.CommandText = $"insert into {tableName} (hash, position) values ({item.Key}, {item.Value})";
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        public void AddPathIndex(ulong pathHash, IItemMetaWithInfo info)
        {
            if (info.Position.HasValue)
            {
                if (info.IsFolder)
                    _folderBatch.Add(new KeyValuePair<ulong, long>(pathHash, info.Position.Value));
                else
                    _fileBatch.Add(new KeyValuePair<ulong, long>(pathHash, info.Position.Value));

                Flush(MinBatch);
            }
        }
    }
}