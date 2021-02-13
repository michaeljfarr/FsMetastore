using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IndexedData;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    public interface IMetaSerializer
    {
        void WriteFile(BinaryWriter stream, FileMeta fileMeta, BatchStorageRules batchStorageRules,
            IStringRefWriter stringRefWriter);
        void WriteFolder(BinaryWriter stream, FolderMeta folderMeta, BatchStorageRules batchStorageRules,
            IStringRefWriter stringRefWriter);
        FolderMeta ReadFolder(BinaryReader reader, BatchStorageRules batchStorageRules);
        FileMeta ReadFile(BinaryReader reader, BatchStorageRules batchStorageRules);
    }
}