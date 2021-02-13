namespace FsMetastore.Model.StorageStrategy
{
    /// <summary>
    /// StringStorageType is written as as a single byte to the binary stream to represent distinct
    /// storage strategies for each string
    /// </summary>
    /// <remarks>
    /// This enables FileMeta and FolderMeta to refer to variable length strings in separate files.
    /// As discussed elsewhere this helps minimize storage and transfer data volume in some situations
    /// at the cost of more CPU and local time on the machine generating the files.
    /// </remarks>
    public enum StringStorageType
    {
        /// <summary>
        /// Note, if Undef is discovered in a data stream it should treat the item as a zero length string.
        /// </summary>
        Undef         = 0x0,
        StringRef     = 0x1,
        LocalString   = 0x2,
    };
}