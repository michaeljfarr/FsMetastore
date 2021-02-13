using System;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.IO.FileBatches
{
    public interface IMetaPersister : IDisposable
    {
        void Open();
        /// <summary>
        /// StoreSourceAsync is the initialization func called by FileMetaSpider before any
        /// storage methods are called.
        /// </summary>
        /// <returns></returns>
        Task<BatchSource> StoreSourceAsync(DriveMeta driveMeta, BatchSourceEncoding batchSourceEncoding,
            BatchInfo batchInfo,
            BatchStatistics batchStatistics, int scanGeneration);
        void StoreFolder(FolderMeta folderMeta);
        void StoreFile(FileMeta fileMeta);
        void Close();
        void RevertNewFolders(long position);
    }
}