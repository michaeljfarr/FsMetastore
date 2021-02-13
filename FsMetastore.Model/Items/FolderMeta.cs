using System;
using System.Diagnostics;

namespace FsMetastore.Model.Items
{
    [DebuggerDisplay("Name = {Name?.Value}, ParentId={" + nameof(ParentId) + "}")]
    public class FolderMeta : IItemMeta
    {
        /// <summary>
        /// An integer that represents the folder within this batch.
        /// </summary>
        public int Id { get; set; } 

        /// <summary>
        /// An integer that represents the folders parent within this batch.
        /// Null if this Meta is the root.
        /// </summary>
        public int? ParentId { get; set; } 

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

        public bool IsFolder => true;
        public long? Position { get; set; }

        public FolderMeta AddPermission(PermissionMask permission)
        {
            PermissionMask = PermissionMask ?? 0;
            PermissionMask |= permission;
            return this;
        }

    }
}