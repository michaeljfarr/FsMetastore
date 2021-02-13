using System;

namespace FsMetastore.Model.StorageStrategy
{
    [Flags]
    public enum FolderMetaValueMask
    {
        SurrogateId    = 0x1,
        ModifiedDate   = 0x1 << 1,
        OwnerId        = 0x1 << 2,
        GroupId        = 0x1 << 3,
        PermissionMask = 0x1 << 4,
    }
}