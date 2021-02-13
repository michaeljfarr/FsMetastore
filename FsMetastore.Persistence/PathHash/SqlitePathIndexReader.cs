using System;
using System.Collections.Generic;
using System.Data;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.Sqlite;

namespace FsMetastore.Persistence.PathHash
{
    class SqlitePathIndexReader : SqlitePathIndexBase, IPathIndexReader
    {
        public SqlitePathIndexReader(BatchIOConfig batchIOConfig): base(batchIOConfig)
        {
        }



        public IEnumerable<long> ReadPotentialFolderPositions(ulong hash)
        {
            return ReadPathIndex(hash, FolderPositionDataTableName);
        }

        public IEnumerable<long> ReadPotentialMetaPositions(ulong hash)
        {
            return ReadPathIndex(hash, MetaPositionDataTableName);
        }

        private IEnumerable<long> ReadPathIndex(ulong hash, string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"select position from {tableName} where hash={hash}";
                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {
                    yield return (long)result[0];
                }
            }
        }

        protected override bool ShouldDeleteOnClose()
        {
            return false;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }
    }
}