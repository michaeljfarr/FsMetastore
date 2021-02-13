using System;

namespace FsMetastore.Model.StorageStrategy
{
    [Flags]
    public enum FileMetaValueMask
    {
        FileLength     = 0x1,
        ModifiedDate   = 0x1 << 1,
        OwnerId        = 0x1 << 2,
        GroupId        = 0x1 << 3,
        PermissionMask = 0x1 << 4,
        SurrogateId    = 0x1 << 5,
        _7             = 0x1 << 6
    }
}