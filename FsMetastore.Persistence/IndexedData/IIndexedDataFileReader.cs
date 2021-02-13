using System;

namespace FsMetastore.Persistence.IndexedData
{
    public interface IIndexedDataFileReader : IDisposable
    {
        bool OpenRead();
        string FindStringItem(uint itemId);
    }
}