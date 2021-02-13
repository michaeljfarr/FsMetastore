using System;
using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.Meta
{
    public interface IMetaFactory
    {
        DriveMeta CreateDrive(DriveInfo driveInfo);
        //DriveMeta CreateDrive(DriveInfo driveInfo, Guid surrogateId);
        FolderMeta CreateFolder(DirectoryInfo directoryInfo, FolderMeta parent, Guid? surrogateId);
        FileMeta CreateFile(FileInfo fileInfo, FolderMeta parent, Guid? surrogateId);
        void SetNextIds(int nextFileId, int nextFolderId);
        BatchInfo GetBatchInfo(int scanGeneration);
        void RecoverId(FolderMeta folderMeta);
    }
}