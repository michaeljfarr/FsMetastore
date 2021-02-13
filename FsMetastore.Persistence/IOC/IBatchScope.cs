using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IndexedData;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.IO.FsMetaDb;

namespace FsMetastore.Persistence.IOC
{
    public enum StringRefInitType
    {
        /// <summary>
        /// Do not open the StringRef database
        /// </summary>
        None,
        /// <summary>
        /// Open the StringRef database and ensure the tables exist.
        /// </summary>
        Init,
        /// <summary>
        /// Delete the StringRef database if it exists, then initialise new one and open it.
        /// </summary>
        Clean
    };
    public interface IBatchScope : IDisposable
    {
        IMetaReader OpenMetaReader();
        IEnumerable<IItemMetaWithInfo> ReadInfos();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="initStringRefs">If true, will </param>
        /// <returns></returns>
        Task<(BatchSource batchSource, BatchStatistics batchStatistics)> CaptureFileMetadata(string sourcePath,
            StringRefInitType initStringRefs);

        Task<IEnumerable<IItemMetaWithInfo>> ReadZip();
        
        // IMetaEnumerator GetMetaEnumerator();
        // IMetaBatchReader GetBatchSourceReader();
        IBatchSourceProvider GetBatchSourceProvider();
        IEnumerable<FileMeta> EnumerateMetadata();
        IEnumerable<FolderMeta> EnumerateFolders();
        void OptimizeStringRefs();
        void WriteIndexedData(Dictionary<uint, string> values);
        IIndexedDataFileReader OpenIndexedDataReader();
        void WritePathIndex();
        // Task<(BatchSource batchSource, BatchStatistics batchStatistics)> CaptureDiff(IBatchScope readScope,
        //     string sourcePath, StringRefInitType initStringRefs);
        IFsMetaDbContext GetImportDbMetaContext();
        Task ExportGenerationAsDiff(IFsMetaDbContext fsMetaDbContext, int generation);
        //Task ImportDiff(IMetaBatchReader metaStreamBatchReader, IMetaEnumerator metaEnumerator);
        Task<(BatchSource batchSource, BatchStatistics batchStatistics)> ImportDiff(BatchSource diffSource,
            IEnumerable<IItemMetaWithInfo> metaEnumerator);
        Task InitSource(int scanGeneration);
    }
}