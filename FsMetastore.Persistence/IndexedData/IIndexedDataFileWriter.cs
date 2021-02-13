using System;

namespace FsMetastore.Persistence.IndexedData
{
    public interface IIndexedDataFileWriter : IDisposable
    {
        void Create();
        void WriteStringItem(uint itemId, string itemValue);
        void Close();
    }
}