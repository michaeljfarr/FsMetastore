using System;

namespace FsMetastore.Model.Items
{
    public interface IItemMeta
    {
        int Id { get; }
        int? ParentId { get; }
        DateTimeOffset? ModifiedDate { get; }
        PermissionMask? PermissionMask { get; }
        int? OwnerId { get; }
        int? GroupId { get; }
        bool IsFolder { get; }
        /// <summary>
        /// When the file is written to a file, the position start of the record (in bytes from start of file)
        /// </summary>
        long? Position { get; }
    }
}