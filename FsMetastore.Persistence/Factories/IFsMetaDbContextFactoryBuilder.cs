using System;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;

namespace FsMetastore.Persistence.Factories
{
    public interface IFsMetaDbContextFactoryBuilder
    {
        IFsMetaDbContextFactory CreateForDrive(Guid driveId);
        Task<BatchSource> ReadSource(string fsMetaDbPath);
    }
}