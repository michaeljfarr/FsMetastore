using System;
using FsMetastore.Model.Batch;

namespace FsMetastore.Model.Items
{
    public class DriveMeta
    {
        /// <summary>
        /// A Guid stored somewhere on the drive to define it globally
        /// </summary>
        public Guid Id { get; set; } 
        /// <summary>
        /// Root folder in the drive.
        /// </summary>
        public FolderMeta RootFolder { get; set; } 
        /// <summary>
        /// A string indicating the mount point of the drive within the system.
        /// </summary>
        public string MountPoint { get; set; } 
        
        public PathCaseRule PathCaseRule { get; set; }
    }
}