using System.Diagnostics;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.IO.FileBatches
{
    public class NoopTimerMetaPersister : IMetaPersister
    {
        private readonly BatchStorageRules _batchStorageRules;

        public NoopTimerMetaPersister(BatchStorageRules batchStorageRules)
        {
            _batchStorageRules = batchStorageRules;
        }

        public Stopwatch Stopwatch { get; private set; }

        public void Open()
        {
        }

        public Task<BatchSource> StoreSourceAsync(DriveMeta driveMeta, BatchSourceEncoding batchSourceEncoding,
            BatchInfo batchInfo, BatchStatistics batchStatistics, int scanGeneration)
        {
            Stopwatch = Stopwatch.StartNew();
            var batchSource = BatchSource.FromDetails(driveMeta, batchSourceEncoding, batchInfo, _batchStorageRules);
            return Task.FromResult(batchSource);
        }

        public void StoreFolder(FolderMeta folderMeta)
        {
            folderMeta.Position = 0;
        }

        public void StoreFile(FileMeta fileMeta)
        {
            
        }

        public void Close()
        {
            Stopwatch.Stop();
        }

        public void RevertNewFolders(long position)
        {
            
        }

        public void Dispose()
        {
        }
    }
}