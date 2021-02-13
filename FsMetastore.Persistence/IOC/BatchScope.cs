using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IndexedData;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.IO.FsMetaDb;
using FsMetastore.Persistence.IO.FsScanStream;
using FsMetastore.Persistence.Meta;
using FsMetastore.Persistence.PathHash;
using FsMetastore.Persistence.Zipper;
using Microsoft.Extensions.DependencyInjection;

namespace FsMetastore.Persistence.IOC
{
    class BatchScope : IBatchScope
    {
        private readonly IServiceScope _scope;
        private readonly StorageType _storageType;
        private bool _metaReaderOpen = false;
        private IMetaEnumerator _metaEnumerator = null;
        
        public BatchScope(IServiceScope scope, StorageType storageType)
        {
            _scope = scope;
            _storageType = storageType;
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }

        public T GetRequiredService<T>()
        {
            return _scope.ServiceProvider.GetRequiredService<T>();
        }

        public IMetaReader OpenMetaReader()
        {
            return OpenMetaReaderInner(false);
        }
        
        private IMetaReader OpenMetaReaderInner(bool forRewrite = false)
        {
            var metaReader = GetMetaReader();
            if (!_metaReaderOpen)
            {
                metaReader.Open(forRewrite);
            }

            _metaReaderOpen = true;
            return metaReader;
        }


        public IEnumerable<IItemMetaWithInfo> ReadInfos()
        {
            var metaEnumerator = GetMetaEnumerator();
            using (var indexedDataFileReader = GetIndexedDataFileReader())
            {
                var hasIndexedNames = indexedDataFileReader.OpenRead();
                foreach (var info in metaEnumerator.ReadInfos())
                {
                    if (hasIndexedNames)
                    {
                        ReadName(indexedDataFileReader, (ItemMetaWithInfo)info);
                    }

                    yield return info;
                }
            }
        }
        
        public static void ReadName(IIndexedDataFileReader indexedDataFileReader, ItemMetaWithInfo start)
        {
            for (var item = start; item != null; item = item.Parent)
            {
                var storedString = (StoredString) null;
                var asFolder = item.AsFolder;
                var asFile = item.AsFolder;
                if (asFolder!=null)
                {
                    storedString = asFolder.Name;
                }
                else if (asFile!=null)
                {
                    storedString = asFile.Name;
                }
                if(storedString!=null && storedString.Value == null && storedString.Id!=null)
                {
                    storedString.Value = indexedDataFileReader.FindStringItem((uint)storedString.Id.Value);
                }
            }
        }

        private IMetaReader GetMetaReader()
        {
            switch (_storageType)
            {
                case StorageType.MetaStream:
                    return GetRequiredService<MetaStreamReader>();
                case StorageType.FileMetaDb:
                    return GetRequiredService<FsMetaDbMetaReader>();
                case StorageType.MetaStreamPlusDb:
                    return GetRequiredService<MetaStreamPlusDbReader>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IMetaPersister OpenMetaPersister()
        {
            var persister = GetMetaPersister();
            persister.Open();
            return persister;
        }

        private IMetaPersister GetMetaPersister()
        {
            switch (_storageType)
            {
                case StorageType.MetaStream:
                    return GetRequiredService<MetaStreamPersister>();
                case StorageType.FileMetaDb:
                    return GetRequiredService<FsMetaDbPersister>();
                case StorageType.NoopTimer:
                    return GetRequiredService<NoopTimerMetaPersister>();
                // case StorageType.ImportPlusMetaStream:
                //     break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IMetaBatchReader GetBatchSourceReader()
        {
            var val = GetRequiredService<IMetaBatchReader>();
            return val;
        }
        
        public IBatchSourceProvider GetBatchSourceProvider()
        {
            var val = GetRequiredService<IBatchSourceProvider>();
            return val;
        }

        public IFsMetaDbContext GetImportDbMetaContext()
        {
            var importDbMetaContext = GetRequiredService<IFsMetaDbContext>();
            return importDbMetaContext;
        }

        public IMetaEnumerator GetMetaEnumerator()
        {
            if (_metaEnumerator == null)
            {
                _metaEnumerator = new MetaEnumerator(GetMetaReader());
            }
            else
            {
                throw new ApplicationException("This might be OK, but isn't tested.");
            }
            return _metaEnumerator;
        }

        public IIndexedDataFileReader GetIndexedDataFileReader()
        {
            var indexedDataFileReader = GetRequiredService<IIndexedDataFileReader>();
            return indexedDataFileReader;
        }

        public async Task<IEnumerable<IItemMetaWithInfo>> ReadZip()
        {
            //var batchIOConfig = GetRequiredService<BatchIOConfig>();
            var batchSourceProvider = GetRequiredService<IBatchSourceProvider>();
            
            var batchSource = await GetRequiredService<IMetaBatchReader>().ReadSourceAsync();

            ((BatchSourceProvider)batchSourceProvider).BatchSource = batchSource;
            var batchIOConfig = GetRequiredService<BatchIOConfig>();
            if (string.IsNullOrEmpty(batchIOConfig.SourcePath))
            {
                if (string.IsNullOrWhiteSpace(batchIOConfig.DiffPartRoot))
                {
                    throw new ApplicationException("Neither SourcePath nor DiffPartRoot is set");
                }
                var metaEnumerator = GetMetaEnumerator();
                return metaEnumerator.ReadInfos();
            }
            else
            {
                var zipper = GetRequiredService<FileSystemZipEnumerableFactory>();
                var metaFactory = GetRequiredService<IMetaFactory>();
                var metaReader = GetMetaReader();
                var metaEnumerator = new MetaEnumerator(metaReader);

                var fileMetaStreamZipper = new FileMetaStreamZipper(
                    metaFactory,
                    batchSourceProvider,
                    zipper.Open(),
                    metaEnumerator.ReadInfos());
                return fileMetaStreamZipper.Process();
            }
        }

        public async Task<(BatchSource batchSource, BatchStatistics batchStatistics)> CaptureFileMetadata(
            string sourcePath, StringRefInitType initStringRefs)
        {
            if (!Directory.Exists(sourcePath))
            {
                throw new ApplicationException($"Unknown source folder {sourcePath}");
            }

            using (var metaWriter = OpenMetaPersister())
            {
                IStringRefWriter stringRefWriter = null;
                if (initStringRefs != StringRefInitType.None)
                {
                    stringRefWriter = GetRequiredService<IStringRefWriter>();
                    if (initStringRefs == StringRefInitType.Clean)
                    {
                        stringRefWriter.DeleteDb();
                    }

                    //this will open the connection and ensure the required tables exist
                    //we don't create indexes for string refs yet, but we would control those here.
                    stringRefWriter.InitDb();
                }
                var batchStatistics = new BatchStatistics { NumFoldersFound = 1 };
                var directoryInfo = new DirectoryInfo(sourcePath);
                var drive = new DriveInfo(directoryInfo.Root.Name);
                var metaFactory = GetRequiredService<IMetaFactory>();
                var driveMeta = metaFactory.CreateDrive(drive);
                
                //metaWriter.Open();
                var scanGeneration = 1;
                var batchInfo = new BatchInfo() {Generation = 0, NextFileId = 0, NextFolderId = 0};
                var batchSource =  
                    await metaWriter.StoreSourceAsync(driveMeta, GetRequiredService<BatchStorageRules>().Encoding, batchInfo, null, scanGeneration);

                var metaEnumerator = new EmptyMetaEnumerator();
                var batchSourceProvider = GetRequiredService<IBatchSourceProvider>();
                ((BatchSourceProvider)batchSourceProvider).BatchSource = batchSource;
                var zipper = GetRequiredService<FileSystemZipEnumerableFactory>();
                
                var fileMetaStreamZipper = new FileMetaStreamZipper(
                    metaFactory,
                    batchSourceProvider,
                    zipper.Open(),
                    metaEnumerator.ReadInfos());
                
                foreach (var meta in fileMetaStreamZipper.Process())
                {
                    var file = meta.AsFile;
                    var folder = meta.AsFolder;
                    if (folder != null)
                    {
                        batchStatistics.NumFoldersFound++;
                        metaWriter.StoreFolder(folder);
                    }
                    else if (file != null)
                    {
                        batchStatistics.NumFilesFound ++;
                        if (!meta.HasPermission(PermissionMask.Unchanged))
                        {
                            batchStatistics.NumFileChanges++;
                        }
                        metaWriter.StoreFile(file);
                    }
                    else
                    {
                        throw new ApplicationException($"Unknown meta {meta}");
                    }
                }

                var finalBatchSource = await metaWriter.StoreSourceAsync(driveMeta, GetRequiredService<BatchStorageRules>().Encoding, metaFactory.GetBatchInfo(scanGeneration), batchStatistics, scanGeneration);

                metaWriter.Close();
                stringRefWriter?.Close();
                return (finalBatchSource, batchStatistics);
            }
        }

        public IEnumerable<FileMeta> EnumerateMetadata()
        {
            var metaReader = OpenMetaReader();
            while (!metaReader.IsFileReaderAtEnd)
            {
                var file = metaReader.ReadNextFile();
                yield return file;
            }
        }

        public IEnumerable<FolderMeta> EnumerateFolders()
        {
            var metaReader = OpenMetaReader();
            while (!metaReader.IsFileReaderAtEnd)
            {
                var folder = metaReader.ReadNextFolder();
                yield return folder;
            }
        }

        public void OptimizeStringRefs()
        {
            var stringRefOptimizer = GetRequiredService<StringRefOptimizer>();

            stringRefOptimizer.Optimize(OpenMetaReaderInner(true), OpenMetaPersister());

        }

        public void WriteIndexedData(Dictionary<uint, string> values)
        {
            var readerWriterFactory = GetRequiredService<IScanDbBatchIOFactory>();

            using (var indexedDataWriter = readerWriterFactory.CreateIndexedDataWriter())
            {
                indexedDataWriter.Create();
                foreach (var pair in values.OrderBy(a => a.Key))
                {
                    indexedDataWriter.WriteStringItem(pair.Key, pair.Value);
                }
            }
        }

        public IIndexedDataFileReader OpenIndexedDataReader()
        {
            var readerWriterFactory = GetRequiredService<IScanDbBatchIOFactory>();
            var indexedDataReader = readerWriterFactory.CreateIndexedDataReader();
            indexedDataReader.OpenRead();
            return indexedDataReader;
        }

        public void WritePathIndex()
        {
            var pathIndexCreator = GetRequiredService<IPathIndexCreator>();
            pathIndexCreator.WritePathIndex(GetMetaEnumerator());
        }

        
        public async Task ExportGenerationAsDiff(IFsMetaDbContext fsMetaDbContext, int generation)
        {
            using (var metaWriter = OpenMetaPersister())
            {
                var folderChanges = fsMetaDbContext.FoldersFromGen(generation);
                foreach (var folderChange in folderChanges)
                {
                    metaWriter.StoreFolder(folderChange);
                }
                
                var fileChanges = fsMetaDbContext.FilesFromGen(generation);
                foreach (var fileChange in fileChanges)
                {
                    metaWriter.StoreFile(fileChange);
                }
                
                var source = await fsMetaDbContext.ReadSourceAsync();
                await metaWriter.StoreSourceAsync(source.Drive, source.StorageRules.Encoding, source.BatchInfo, null, generation);
            }
        }
        
        // public async Task ImportDiff(IMetaBatchReader metaStreamBatchReader, IMetaEnumerator metaEnumerator)
        // {
        //     var diffSource = await metaStreamBatchReader.ReadSourceAsync();
        //     
        //     var batchSource = await GetRequiredService<IMetaBatchReader>().ReadSourceAsync();
        //
        //     if (diffSource?.BatchInfo?.Generation == null)
        //     {
        //         throw new ApplicationException(
        //             $"Incoming diff had unspecified generation.");
        //     }
        //     if (batchSource?.BatchInfo?.Generation == null)
        //     {
        //         throw new ApplicationException(
        //             $"Existing import db had unspecified generation.");
        //     }
        //
        //     if (batchSource.BatchInfo.Generation != (diffSource.BatchInfo.Generation - 1))
        //     {
        //         throw new ApplicationException(
        //             $"Incoming diff had unexpected generation {diffSource.BatchInfo.Generation} when we have {batchSource.BatchInfo.Generation}.");
        //     }
        //     
        //     if (batchSource.Drive.MountPoint != diffSource.Drive.MountPoint)
        //     {
        //         throw new ApplicationException(
        //             $"Incoming diff had unexpected drive {diffSource.Drive.MountPoint} when we have {batchSource.Drive.MountPoint}.");
        //     }
        //     
        //     GetRequiredService<BatchSourceProvider>().BatchSource = batchSource;
        //
        //     using (var metaWriter = OpenMetaPersister())
        //     {
        //         foreach (var meta in metaEnumerator.ReadInfos())
        //         {
        //             var file = meta.AsFile;
        //             var folder = meta.AsFolder;
        //             if (folder != null)
        //             {
        //                 metaWriter.StoreFolder(folder);
        //             }
        //             else if (file != null)
        //             {
        //                 metaWriter.StoreFile(file);
        //             }
        //             else
        //             {
        //                 throw new ApplicationException($"Unknown meta {meta}");
        //             }
        //         }
        //
        //         await metaWriter.StoreSourceAsync(batchSource.Drive, batchSource.StorageRules.Encoding, diffSource.BatchInfo, null, diffSource.BatchInfo.Generation);
        //     }
        // }

        public async Task<(BatchSource batchSource, BatchStatistics batchStatistics)> ImportDiff(BatchSource diffSource,
            IEnumerable<IItemMetaWithInfo> metaEnumerator)
        {
            var batchStatistics = new BatchStatistics { NumFoldersFound = 1 };
            var batchSource = await GetRequiredService<IMetaBatchReader>().ReadSourceAsync();

            if (diffSource?.BatchInfo?.Generation == null)
            {
                throw new ApplicationException(
                    $"Incoming diff had unspecified generation.");
            }
            if (batchSource?.BatchInfo?.Generation == null)
            {
                throw new ApplicationException(
                    $"Existing import db had unspecified generation.");
            }

            if (batchSource.BatchInfo.Generation != (diffSource.BatchInfo.Generation - 1))
            {
                throw new ApplicationException(
                    $"Incoming diff had unexpected generation {diffSource.BatchInfo.Generation} when we have {batchSource.BatchInfo.Generation}.");
            }
            
            if (batchSource.Drive.MountPoint != diffSource.Drive.MountPoint)
            {
                throw new ApplicationException(
                    $"Incoming diff had unexpected drive {diffSource.Drive.MountPoint} when we have {batchSource.Drive.MountPoint}.");
            }
            
            GetRequiredService<BatchSourceProvider>().BatchSource = batchSource;

            using (var metaWriter = OpenMetaPersister())
            {
                foreach (var meta in metaEnumerator)
                {
                    if (!meta.HasPermission(PermissionMask.Unchanged))
                    {
                        var file = meta.AsFile;
                        var folder = meta.AsFolder;
                        if (folder != null)
                        {
                            metaWriter.StoreFolder(folder);
                            batchStatistics.NumFoldersFound++;
                        }
                        else if (file != null)
                        {
                            metaWriter.StoreFile(file);
                            batchStatistics.NumFileChanges++;
                            if (file.PermissionMask?.HasFlag(PermissionMask.Unchanged) != true)
                            {
                                batchStatistics.NumFileChanges++;
                            }                            
                        }
                        else
                        {
                            throw new ApplicationException($"Unknown meta {meta}");
                        }
                    }
                }

                await metaWriter.StoreSourceAsync(batchSource.Drive, batchSource.StorageRules.Encoding, diffSource.BatchInfo, null, diffSource.BatchInfo.Generation);
            }
            return (batchSource: diffSource, batchStatistics); 
        }

        public async Task InitSource(int scanGeneration)
        {
            var batchIOConfig = GetRequiredService<BatchIOConfig>();

            var batchStorageRules = GetRequiredService<BatchStorageRules>();

            using (var metaWriter = OpenMetaPersister())
            {
                var dirInfo = new DirectoryInfo(batchIOConfig.BatchPathRoot);
                var driveInfo = new DriveInfo(dirInfo.Root.FullName);
                var metaFactory = GetRequiredService<IMetaFactory>();
                var driveMeta = metaFactory.CreateDrive(driveInfo);
                await metaWriter.StoreSourceAsync(driveMeta, batchStorageRules.Encoding, new BatchInfo(), null, scanGeneration);
            }
        }
    }
}