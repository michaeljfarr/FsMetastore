using System.IO;
using FsMetastore.Model.Batch;

namespace FsMetastore.Persistence.Sqlite
{
    abstract class SqlitePathIndexBase : SqliteBase
    {
        protected const string FolderPositionDataTableName = "FolderPositionLookup";
        protected const string MetaPositionDataTableName = "MetaPositionLookup";
        public SqlitePathIndexBase(BatchIOConfig batchIOConfig)
        {
            //if we have about 2M files per windows file system and we use 8 bytes for the hash and 8 for the index, then 16MB of data + 16MB of index ... prob <32MB on disk
            //its a bit stupid, we should be able to find a system that just stores a sorted index
            //also note that Sqlite doesn't actually stores data in magnitude dependent fashion.
            //The value is a signed integer, stored in 1, 2, 3, 4, 6, or 8 bytes depending on the magnitude of the value.
            var targetFolder = batchIOConfig.BatchPathRoot;
            _filePath = Path.Combine(targetFolder, "pathindex.db");
        }

        public void InitDb()
        {
            OpenConnection();
            EnsureTablesExists();

            //lets avoid maintaining indexes while we are just doing bulk inserts, we can recreate it at the end.
            DropAllIndexes(FolderPositionDataTableName);
        }

        private void EnsureTablesExists()
        {
            base.EnsureTableExists(FolderPositionDataTableName, "(hash INTEGER, position INTEGER)");
            base.EnsureTableExists(MetaPositionDataTableName, "(hash INTEGER, position INTEGER)");
        }

    }
}