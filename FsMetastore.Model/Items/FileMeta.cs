using System;

namespace FsMetastore.Model.Items
{
    /*
        * Id: (An n byte integer that represents the folder within this batch.)
        * Name 
            * Potentially a "string ref", see notes below.
        * Mask: (A byte bitmask indicating which of the remaining values to expect)
        * SurrogateId: (If the source file system knows it.)
        * Modified Date
        * File Length
        * Permission Mask
        * Owner Id
        * Group Id
     */
    public class FileMeta : IItemMeta
    {
        /// <summary>
        /// An integer that identifies the file within this batch.
        /// </summary>
        public int Id { get; set; } 

        /// <summary>
        /// An integer that identifies the file's folders within this batch.
        /// </summary>
        public int FolderId { get; set; }

        public int? ParentId => FolderId;

        /// <summary>
        /// This will be null/empty if the ParentId is the drive.
        /// </summary>
        public StoredString Name { get; set; } 

        // <summary>
        // This mask indicates the values that should be stored.
        // </summary>
        // public FolderMetaValueMask ValueMask { get; set; }
        
        /// <summary>
        /// A guid representing the object globally - if one exists and is known at the source.
        /// </summary>
        public Guid? SurrogateId { get; set; }

        public DateTimeOffset? ModifiedDate { get; set; }
        public PermissionMask? PermissionMask { get; set; }
        public int? OwnerId { get; set; }
        public int? GroupId { get; set; }

        public long? FileLength { get; set; }

        public bool IsFolder => false;
        public long? Position { get; set; }
        
        public FileMeta AddPermission(PermissionMask permission)
        {
            PermissionMask = PermissionMask ?? 0;
            PermissionMask |= permission;
            return this;
        }
    }
}