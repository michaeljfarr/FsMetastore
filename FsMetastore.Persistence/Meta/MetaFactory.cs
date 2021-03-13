using System;
using System.IO;
using FsMetastore.Model;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Model.StorageStrategy;

namespace FsMetastore.Persistence.Meta
{
    class MetaFactory : IMetaFactory
    {
        private int _nextFolderId = 1;
        private int _nextFileId = 1;
        //private int _nextStringId = 1;
        public DriveMeta CreateDrive(DriveInfo driveInfo, Guid surrogateId)
        {
            var collation = GetCollation(driveInfo.DriveFormat);

            var meta = new DriveMeta()
            {
                Id = surrogateId,
                RootFolder = CreateRootFolder(driveInfo, surrogateId),
                MountPoint = driveInfo.RootDirectory.FullName,
                PathCaseRule = collation
            };
            return meta;
        }
        public DriveMeta CreateDrive(DriveInfo driveInfo)
        {
            var surrogateId = EnsureDriveId(driveInfo);
            return CreateDrive(driveInfo, surrogateId);
        }
        
        private static Guid EnsureDriveId(DriveInfo drive)
        {
            //hrm, so technically users are case sensitive on unix ... but not on windows
            var driveJsonPath = Path.Combine(drive.RootDirectory.FullName, ItemFileNames.DriveMetaFileName);
            if (!File.Exists(driveJsonPath))
            {
                File.WriteAllText(driveJsonPath, $@"{{
  ""id"": ""{Guid.NewGuid()}""
}}");
            }
            var driveJson = System.Text.Json.JsonDocument.Parse(File.ReadAllText(driveJsonPath));
            var driveId = driveJson.RootElement.GetProperty("id").GetGuid();
            return driveId;
        }

        private FolderMeta CreateRootFolder(DriveInfo driveInfo, Guid surrogateId)
        {
            return new FolderMeta()
            {
                ParentId = null,
                Name = new StoredString()
                {
                    StorageType = StringStorageType.Undef,
                    Value = driveInfo.Name?.TrimEnd(Constants.SlashChars)
                },
                SurrogateId = surrogateId,
                ModifiedDate = driveInfo.RootDirectory.LastWriteTime,
                PermissionMask = 0,
                GroupId = null,//always null on windows
                OwnerId = null,//always null on windows
                Id = _nextFolderId ++
            };
        }

        private PathCaseRule GetCollation(string driveFormat)
        {
            if (driveFormat == "FAT32")
            {
                return PathCaseRule.Insensitive;
            }
            if (driveFormat == "NTFS")
            {
                return PathCaseRule.Ntfs;
            }

            //not sure how to do Mac folder types yet   
            // if (driveFormat == "HFS+ || Mac OS Extended" || driveFormat == "APFS")
            // {
            //     return PathCaseRule.Preserving;
            // }

            if (driveFormat == "EXT4" || driveFormat == "EXT3" || driveFormat == "EXT2")
            {//also HFS+ (Case-sensitive) /APFS Case-sensitive
                return PathCaseRule.Sensitive;
            }
            

            throw new ApplicationException($"Unknown drive format {driveFormat}");
        }

        public FolderMeta CreateFolder(DirectoryInfo directoryInfo, FolderMeta parent, Guid? surrogateId)
        {
            return new FolderMeta()
            {
                ParentId = parent.Id,
                Name = new StoredString()
                {
                    StorageType = StringStorageType.Undef,
                    Value = directoryInfo.Name
                },
                SurrogateId = surrogateId,
                ModifiedDate = directoryInfo.LastWriteTime,
                PermissionMask = 0,
                GroupId = null, //always null on windows
                OwnerId = null, //always null on windows
                Id = _nextFolderId++
            };
        }

        public FileMeta CreateFile(FileInfo fileInfo, FolderMeta parent, Guid? surrogateId)
        {
            return new FileMeta()
            {
                FolderId = parent.Id,
                Name = new StoredString()
                {
                    StorageType = StringStorageType.Undef,
                    Value = fileInfo.Name
                },
                SurrogateId = surrogateId,
                ModifiedDate = fileInfo.LastWriteTime,
                PermissionMask = null,
                GroupId = null, //always null on windows
                OwnerId = null, //always null on windows
                Id = _nextFileId++,
                FileLength = fileInfo.Exists ? fileInfo.Length : -1
            };
        }

        public void SetNextIds(int nextFileId, int nextFolderId)
        {
            _nextFileId = nextFileId;
            _nextFolderId = nextFolderId;
        }

        public BatchInfo GetBatchInfo(int scanGeneration)
        {
            return new BatchInfo()
            {
                NextFileId = _nextFileId,
                NextFolderId = _nextFolderId,
                Generation = scanGeneration
            };
        }

        public void RecoverId(FolderMeta folderMeta)
        {
            if (_nextFolderId == folderMeta.Id + 1)
            {
                _nextFolderId--;
            }
            else
            {
                
            }
        }
    }
}