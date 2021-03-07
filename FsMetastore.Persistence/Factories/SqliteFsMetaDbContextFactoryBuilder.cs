using System;
using System.IO;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.IO.FsMetaDb;
using FsMetastore.Persistence.IO.Test;
using Microsoft.Extensions.Options;

namespace FsMetastore.Persistence.Factories
{
    class SqliteFsMetaDbContextFactoryBuilder : IFsMetaDbContextFactoryBuilder
    {
        private readonly IOptionsMonitor<FsMetaDbOptions> _config;

        public SqliteFsMetaDbContextFactoryBuilder(IOptionsMonitor<FsMetaDbOptions> config)
        {
            _config = config;
            if (!Directory.Exists(_config.CurrentValue.ConnectionString))
            {
                throw new ApplicationException(
                    $"{nameof(FsMetaDbType.Sqlite)} configuration couldn't find base directory: {_config.CurrentValue.ConnectionString}");
            }
        }

        public IFsMetaDbContextFactory CreateForDrive(Guid driveId)
        {
            var baseFolder = PathToDriveFsMetaDb(driveId);
            return new SqliteFsMetaDbContextFactory(baseFolder);
        }

        public string PathToDriveFsMetaDb(Guid driveId)
        {
            var baseFolder = Path.Combine(_config.CurrentValue.ConnectionString, driveId.ToString("N"));
            return baseFolder;
        }

        public async Task<BatchSource> ReadSource(string fsMetaDbPath)
        {
            var batchIoConfig = new BatchIOConfig
            {
                BatchPathRoot = fsMetaDbPath,
                BatchSourceEncoding = BatchSourceEncoding.Utf8,
                SourcePath = null
            };
            var scanDbBatchIOFactory =
                new IO.FsScanStream.FsScanStreamBatchIOFactory(batchIoConfig,
                    new NullTestOutputer());
            var metaBatchReader = new MetaBatchReader(scanDbBatchIOFactory);
            return await metaBatchReader.ReadSourceAsync();
        }
    }
}