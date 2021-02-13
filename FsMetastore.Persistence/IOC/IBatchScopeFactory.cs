using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.StorageStrategy;

namespace FsMetastore.Persistence.IOC
{
    public interface IBatchScopeFactory
    {
        IBatchScope CreateWriteScope(string batchPathRoot, StorageType storageType,
            NameStorageStrategy nameStorageStrategy, string sourcePath,
            BatchSourceEncoding encoding = BatchSourceEncoding.Utf8);
        Task<IBatchScope> CreateReadScopeAsync(string batchPathRoot, string diffPartRoot = null, string sourceFolder = null);
        Task ExportMetaStream(string dbFolderPath, string exportFolder, int generation);
        Task<(BatchSource batchSource, BatchStatistics batchStatistics)> ImportFromFileSystem(string dbFolderPath,
            string sourcePath, StorageType storageType);

        Task ImportFromMetaStream(string dbFolder, string diffDbFolderToImport);
    }
}