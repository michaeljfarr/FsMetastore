using System;

namespace FsMetastore.Persistence.Factories
{
    public interface IFsMetaDbContextFactoryBuilder
    {
        IFsMetaDbContextFactory CreateForDrive(Guid driveId);
    }
}