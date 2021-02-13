using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IO.FileBatches;
using Microsoft.Extensions.DependencyInjection;

namespace FsMetastore.Persistence.IOC
{
    class BatchScopeFactory : IBatchScopeFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BatchScopeFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public IBatchScope CreateWriteScope(string batchPathRoot, StorageType storageType,
            NameStorageStrategy nameStorageStrategy, string sourcePath,
            BatchSourceEncoding encoding = BatchSourceEncoding.Utf8)
        {
            var scope = _scopeFactory.CreateScope();
            var batchIOConfig = scope.ServiceProvider.GetRequiredService<BatchIOConfig>();
            batchIOConfig.BatchPathRoot = batchPathRoot;
            batchIOConfig.BatchSourceEncoding = encoding;
            batchIOConfig.SourcePath = sourcePath;

            var batchStorageRules = scope.ServiceProvider.GetRequiredService<BatchStorageRules>();
            batchStorageRules.Encoding = encoding;
            batchStorageRules.NameStorageStrategy = nameStorageStrategy;
            if(nameStorageStrategy == NameStorageStrategy.StringRef)
            {
                batchStorageRules.FolderMetaValueMask = FolderMetaValueMask.ModifiedDate;
                batchStorageRules.FileMetaValueMask = FileMetaValueMask.FileLength |
                                                      FileMetaValueMask.ModifiedDate;
            }
            return new BatchScope(scope, storageType);
        }

        public async Task ExportMetaStream(string importDbFolderPath, string diffFolderPath, int generation)
        {
            using (var readScope = await CreateReadScopeAsync(importDbFolderPath, null, null))
            using (var writeScope =
                CreateWriteScope(diffFolderPath, StorageType.MetaStream, NameStorageStrategy.LocalString, null))
            {
                var importDbContext = readScope.GetImportDbMetaContext();
                await writeScope.ExportGenerationAsDiff(importDbContext, generation);
            }
        }
        
        // public async Task ImportDiff(string dbFolderPath, string diffFolderPath)
        // {
        //     using (var readScope = await CreateReadScopeAsync(diffFolderPath, null, null))
        //     using (var writeScope = CreateWriteScope(dbFolderPath, StorageType.FileMetaDb, NameStorageStrategy.LocalString, null))
        //     {
        //         var batchSourceReader = readScope.GetBatchSourceReader();
        //         var metaEnumerator = readScope.GetMetaEnumerator();
        //         await writeScope.ImportDiff(batchSourceReader, metaEnumerator);
        //     }
        // }
        
        public async Task<(BatchSource batchSource, BatchStatistics batchStatistics)> ImportFromFileSystem(string dbFolderPath,
            string sourcePath, StorageType storageType)
        {
            return await Import(dbFolderPath, null, sourcePath, storageType);
        }

        private async Task<(BatchSource batchSource, BatchStatistics batchStatistics)> Import(string dbFolderPath, string diffPartRoot, string sourcePath, StorageType storageType)
        {
            var readScope = await CreateReadScopeAsync(dbFolderPath, diffPartRoot, sourcePath);
            if (readScope == null)
            {
                readScope = CreateWriteScope(dbFolderPath, storageType,
                    NameStorageStrategy.LocalString, sourcePath);
                await readScope.InitSource(0);
            }

            using (readScope)
            using (var writeScope =
                CreateWriteScope(dbFolderPath, storageType, NameStorageStrategy.LocalString, sourcePath))
            {
                var metaEnumerator = await readScope.ReadZip();
                var batchSourceProvider = readScope.GetBatchSourceProvider();
                var diffSource = batchSourceProvider.BatchSource;
                diffSource.BatchInfo.Generation += 1;
                return await writeScope.ImportDiff(diffSource, metaEnumerator);
            }
        }

        public Task ImportFromMetaStream(string dbFolder, string diffDbFolderToImport)
        {
            return Import(dbFolder, diffDbFolderToImport, null, StorageType.FileMetaDb);
        }


        public async Task<IBatchScope> CreateReadScopeAsync(string batchPathRoot, string diffPartRoot,
            string sourcePath)
        {
            var scope = _scopeFactory.CreateScope();
            
            var batchIOConfig = scope.ServiceProvider.GetRequiredService<BatchIOConfig>();
            batchIOConfig.BatchPathRoot = batchPathRoot;
            batchIOConfig.DiffPartRoot = diffPartRoot;
            batchIOConfig.SourcePath = sourcePath;
            var batchSourceReader = scope.ServiceProvider.GetRequiredService<IMetaBatchReader>();
            var batchStorageRules = scope.ServiceProvider.GetRequiredService<BatchStorageRules>();

            var batchSource = await batchSourceReader.ReadSourceAsync();
            if (batchSource == null)
            {
                return null;
            }

            var batchSourceProvider = scope.ServiceProvider.GetRequiredService<BatchSourceProvider>();
            batchSourceProvider.BatchSource = batchSource; 

            batchIOConfig.BatchSourceEncoding = batchSource.StorageRules.Encoding;
            batchStorageRules.Encoding = batchSource.StorageRules.Encoding;
            batchStorageRules.NameStorageStrategy = batchSource.StorageRules.NameStorageStrategy;
            batchStorageRules.FileMetaValueMask = batchSource.StorageRules.FileMetaValueMask;
            batchStorageRules.FolderMetaValueMask = batchSource.StorageRules.FolderMetaValueMask;

            var metaStreamFileNames = BatchFileNames.MetaStreamFileNames();
            var importDbFileName = BatchFileNames.FileMetaDbFileName;
            var storageType = StorageType.Unset;
            var importDbFileExists = File.Exists(Path.Combine(batchPathRoot, importDbFileName));
            if (metaStreamFileNames.All(fn => File.Exists(Path.Combine(batchPathRoot, fn))))
            {
                // if(importDbFileExists)
                // {
                //     storageType = StorageType.MetaStreamPlusDb;
                // }
                // else
                // {
                //     storageType = StorageType.MetaStream;
                // }
                storageType = StorageType.MetaStream;
            }
            else if(importDbFileExists)
            {
                storageType = StorageType.FileMetaDb;
            }

            if (storageType != StorageType.FileMetaDb && storageType != StorageType.MetaStream )//&& storageType != StorageType.MetaStreamPlusDb)
            {
                throw new ApplicationException($"Unsupported StorageType: {storageType}");
            }

            if (!string.IsNullOrWhiteSpace(diffPartRoot) && storageType == StorageType.MetaStream)
            {
                storageType = StorageType.MetaStreamPlusDb;
            }

            return new BatchScope(scope, storageType);
        }
    }
}
