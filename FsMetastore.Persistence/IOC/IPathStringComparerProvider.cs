using System;

namespace FsMetastore.Persistence.IOC
{
    public interface IPathStringComparerProvider
    {
        StringComparer StringComparer { get; }
    }
}