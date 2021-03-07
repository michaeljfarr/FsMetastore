using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.IO.FsMetaDb;
using FsMetastore.Persistence.IO.Test;

namespace FsMetastore.Persistence.Factories
{
    class SqliteFsMetaDbContextFactory : IFsMetaDbContextFactory
    {
        private readonly string _baseFolder;

        public SqliteFsMetaDbContextFactory(string baseFolder)
        {
            _baseFolder = baseFolder;
        }

        public IFsMetaDbContext Create()
        {
            var batchIoConfig = new BatchIOConfig
            {
                BatchPathRoot = _baseFolder,
                BatchSourceEncoding = BatchSourceEncoding.Utf8,
                SourcePath = null
            };
            var scanDbBatchIOFactory =
                new IO.FsScanStream.FsScanStreamBatchIOFactory(batchIoConfig,
                    new NullTestOutputer());
            var metaBatchReader = new MetaBatchReader(scanDbBatchIOFactory);
            return new FsMetaDbContext(batchIoConfig, metaBatchReader);
        }
    }
}