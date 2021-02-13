using System;
using FsMetastore.Model.Items;

namespace FsMetastore.Model.Batch
{
    /// <summary>
    /// This provides an overall description of the data in terms of the origin of the information and the BatchStorageRules
    /// </summary>
    public class BatchSource
    {
        public string MachineName { get; set; }
        public DateTimeOffset Date { get; set; }
        public DriveMeta Drive { get; set; }
        public BatchStorageRules StorageRules { get; set; }
        public BatchInfo BatchInfo { get; set; }

        public static BatchSource FromDetails(DriveMeta driveMeta,
            BatchSourceEncoding batchSourceEncoding, BatchInfo batchInfo, BatchStorageRules batchStorageRules)
        {
            var batchSource = new BatchSource()
            {
                Date = DateTimeOffset.UtcNow,
                Drive = driveMeta,
                MachineName = Environment.MachineName,
                StorageRules = new BatchStorageRules()
                {
                    Encoding = batchSourceEncoding,
                    NameStorageStrategy = batchStorageRules.NameStorageStrategy,
                    FileMetaValueMask = batchStorageRules.FileMetaValueMask,
                    FolderMetaValueMask = batchStorageRules.FolderMetaValueMask
                },
                BatchInfo = batchInfo
            };
            return batchSource;
        }
    }
}
