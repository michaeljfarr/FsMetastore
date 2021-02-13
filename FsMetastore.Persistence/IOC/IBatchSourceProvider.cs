using FsMetastore.Model.Batch;

namespace FsMetastore.Persistence.IOC
{
    public interface IBatchSourceProvider : IPathStringComparerProvider
    {
        BatchSource BatchSource { get; }
    }
}