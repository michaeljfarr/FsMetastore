using FsMetastore.Persistence.IO.FsMetaDb;

namespace FsMetastore.Persistence.Factories
{
    public interface IFsMetaDbContextFactory
    {
        IFsMetaDbContext Create();
    }
}