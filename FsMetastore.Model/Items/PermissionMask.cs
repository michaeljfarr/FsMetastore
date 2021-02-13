using System;

namespace FsMetastore.Model.Items
{
    /// <summary>
    /// When collecting information from a linux system we allow some space to store the basic permission mask.
    /// Annoyingly this is 1.125 bytes of information, so we store it as 2 bytes.
    /// </summary>
    /// <remarks>
    /// I have used the reference for Tar (search for TOEXEC here:
    /// https://www.gnu.org/software/tar/manual/html_node/Standard.html)
    /// </remarks>
    [Flags]
    public enum PermissionMask
    {
        WorldExecute = 0x1,
        WorldWrite   = 0x1 << 1,
        WorldRead    = 0x1 << 2,
        GroupExecute = 0x1 << 3,
        GroupWrite   = 0x1 << 4,
        GroupRead    = 0x1 << 5,
        UserWrite    = 0x1 << 6,
        UserExecute  = 0x1 << 7,
        UserRead     = 0x1 << 8,
        Deleted      = 0x1 << 9,
        Unchanged    = 0x1 << 10,
    }
}