using System;
using FsMetastore.Model.Batch;
using FsMetastore.Model.StorageStrategy;

namespace FsMetastore.Model.Items.FileMetaDb
{
    public class Source
    {
        public Guid Id { get; set; } 
        public string MachineName { get; set; }
        public DateTimeOffset Date { get; set; }
        public string MountPoint { get; set; }
        
        public PathCaseRule PathCaseRule { get; set; }
        public FolderMetaValueMask? FolderMetaValueMask { get; set; }
        public FileMetaValueMask? FileMetaValueMask{ get; set; }
    }
}