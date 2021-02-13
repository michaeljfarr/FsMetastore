using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.Sqlite;

namespace FsMetastore.Persistence.IO.FsMetaDb
{
    abstract class FsMetaDbSqliteBase : SqliteBase
    {
        private readonly BatchIOConfig _batchIOConfig;
        protected const string JsonTableName = "JsonMeta";
        protected const string FolderMetaTableName = "FolderMeta";
        protected const string FileMetaTableName = "FileMeta";
        protected const string SourceTableName = "Source";
        protected const string GenerationTableName = "Generation";
        

        public FsMetaDbSqliteBase(BatchIOConfig batchIOConfig)
        {
            _batchIOConfig = batchIOConfig;
            SetFilePath(false);
        }

        protected void SetFilePath(bool useDiff)
        {
            //var targetFolder = (useDiff && _batchIOConfig.DiffPartRoot!=null) ? _batchIOConfig.DiffPartRoot : _batchIOConfig.BatchPathRoot;
            var targetFolder = useDiff ? _batchIOConfig.DiffPartRoot : _batchIOConfig.BatchPathRoot;
            _filePath = Path.Combine(targetFolder, BatchFileNames.FileMetaDbFileName);
        }

        protected override bool ShouldDeleteOnClose()
        {
            return false;
        }
        public override void Flush()
        {
            //readers do not need to implement flush.
            
        }
    }
}