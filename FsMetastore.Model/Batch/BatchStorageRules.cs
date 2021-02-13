using FsMetastore.Model.StorageStrategy;

namespace FsMetastore.Model.Batch
{
    /// <summary>
    /// BatchStorageRules specifies what options of what information is stored and some options within the pattern
    /// of storage.  
    /// </summary>
    public class BatchStorageRules
    {
        /// <summary>
        /// Which optional details are stored with the folder metadata (Date, Owner, Permissions)
        /// </summary>
        public FolderMetaValueMask? FolderMetaValueMask { get; set; }
        /// <summary>
        /// Which optional details are stored with the file metadata (Length, Date, Owner, Permissions)
        /// </summary>
        public FileMetaValueMask? FileMetaValueMask{ get; set; }
        public BatchSourceEncoding Encoding { get; set; }
        /// <summary>
        /// Whether the name is stored long side the file metadata or in another file (not relevant to ImportDb)
        /// </summary>
        public NameStorageStrategy NameStorageStrategy { get; set; }
    }
}