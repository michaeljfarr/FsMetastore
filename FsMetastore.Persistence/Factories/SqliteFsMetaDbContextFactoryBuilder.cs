using System;
using System.IO;
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
            var baseFolder = Path.Combine(_config.CurrentValue.ConnectionString, driveId.ToString("N"));
            return new SqliteFsMetaDbContextFactory(baseFolder);
        }
    }
}