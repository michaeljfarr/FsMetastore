using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IOC;

namespace FsMetastore.Persistence.IO.Commands
{
    public class BatchCommandService : IBatchCommandService
    {
        private readonly IBatchScopeFactory _batchScopeFactory;

        public BatchCommandService(IBatchScopeFactory batchScopeFactory)
        {
            _batchScopeFactory = batchScopeFactory;
        }
        
        public async Task<(BatchSource batchSource, BatchStatistics batchStatistics)> CreateImportDb(string dbFolder,
            string sourcePath, BatchCommandType batchCommandType)
        {
            var directory = new DirectoryInfo(dbFolder);
            if (directory.Exists)
            {
                var filePaths = new[] { 
                    Path.Combine(directory.FullName, BatchFileNames.CreateFileName(MetaFileType.Source, BatchFileNames.JsonSuffix)),
                    Path.Combine(directory.FullName, BatchFileNames.FileMetaDbFileName)
                };
                var existingFiles = filePaths.Where(File.Exists).ToList();
                if (existingFiles.Any() && batchCommandType == BatchCommandType.WipeExisting)
                {
                    foreach (var existingFile in existingFiles)
                    {
                        File.Delete(existingFile);
                    }
                }
                if (existingFiles.Any() && batchCommandType == BatchCommandType.ThrowIfExists)
                {
                    throw new ApplicationException($"Unexpected files within {dbFolder}");
                }
                if (!existingFiles.Any() && batchCommandType == BatchCommandType.ApplyDiff)
                {
                    throw new ApplicationException($"Unexpected files within {dbFolder}");
                }
                if (existingFiles.Any() && (batchCommandType == BatchCommandType.ApplyDiffIfExists || batchCommandType == BatchCommandType.ApplyDiff))
                {
                    if (existingFiles.Count != filePaths.Length)
                    {
                        throw new ApplicationException($"Missing file within {dbFolder}");
                    }
                    //BatchCommandType.ApplyDiffIfExists BatchCommandType.ApplyDiff

                    await _batchScopeFactory.ImportFromFileSystem(dbFolder, sourcePath, StorageType.FileMetaDb);
                    // using (var writeScope = _batchScopeFactory.CreateWriteScope(dbFolder, StorageType.FileMetaDb, NameStorageStrategy.LocalString, sourcePath))
                    // {
                    //     var res = await writeScope.ReadZip().CaptureDiff(writeScope, sourcePath, StringRefInitType.None);
                    //     return res;
                    // }
                }
            }
            else
            {
                if (batchCommandType == BatchCommandType.ApplyDiff)
                {
                    throw new ApplicationException($"Missing folder {dbFolder}");
                }

                if (!directory.Exists)
                {
                    directory.Create();
                }
            }
            
            //BatchCommandType.ApplyDiffIfExists, BatchCommandType.ThrowIfExists BatchCommandType.WipeExisting
            return await _batchScopeFactory.ImportFromFileSystem(dbFolder, sourcePath, StorageType.FileMetaDb);
            // using (var writeScope = _batchScopeFactory.CreateWriteScope(dbFolder, StorageType.FileMetaDb, NameStorageStrategy.LocalString, sourcePath))
            // {
            //     var readStats = await writeScope.CaptureFileMetadata(sourcePath, StringRefInitType.None);
            //     return readStats;
            // }
        }

        public Task ExportChangesAsMetaStream(string dbFolder, string exportFolder, int generation)
        {
            return _batchScopeFactory.ExportMetaStream(dbFolder, exportFolder, generation);
        }

        public Task ImportChangesFromMetaStream(string dbFolder, string diffDbFolderToImport)
        {
            
            return _batchScopeFactory.ImportFromMetaStream(dbFolder, diffDbFolderToImport);
        }
    }
}