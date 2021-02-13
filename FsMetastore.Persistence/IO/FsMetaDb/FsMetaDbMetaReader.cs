using System;
using System.Collections.Generic;
using System.Data;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.Sqlite;

namespace FsMetastore.Persistence.IO.FsMetaDb
{
    /// <summary>
    /// ImportDbMetaReader reads file and folders information from a Sqlite database where files and folders are stored 
    /// in separate tables.
    /// </summary>
    class FsMetaDbMetaReader : FsMetaDbSqliteBase, IMetaReader
    {
        private IEnumerator<(long, long?, string, string, long?, long?, int?, int?, long?)> _fileMetaEnumerator;
        private IEnumerator<(long, long?, string, string, long?, long?, int?, int?, long?)> _folderMetaEnumerator;

        public FsMetaDbMetaReader(BatchIOConfig batchIOConfig): base(batchIOConfig)
        {
        }
        
        public void Dispose()
        {
            Close();
        }

        public bool IsFolderReaderAtEnd { get; private set; } = false;
        public bool IsFileReaderAtEnd { get; private set; } = false;

        public void ReadFromDiff()
        {
            base.SetFilePath(true);
        }
        
        public bool Open(bool forRewrite = false)
        {
            OpenConnection();
            _fileMetaEnumerator = CreateMetaEnumerator(FileMetaTableName).GetEnumerator();
            _folderMetaEnumerator = CreateMetaEnumerator(FolderMetaTableName).GetEnumerator();
            return true;
        }

        private IEnumerable<(long, long?, string, string, long?, long?, int?, int?, long?)> CreateMetaEnumerator(string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                var isFile = tableName == FileMetaTableName;
                var lengthField = isFile ? ", FileLength" : "";
                var order = isFile ? "Id" : "Ord, Id";

                command.CommandText = $"Select Id, ParentId, Name, SurrogateId, ModifiedDate, PermissionMask, OwnerId, GroupId{lengthField} from {tableName} order by {order}";
                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {                   //id 1                   parent id  2             name    3            surrogate  4         modified date 5            permissionmask 6           ownerid 7                groupid 8
                    yield return (result.GetInt32(0), result.GetIntOrNull(1), result[2] as string, result[3] as string, result.GetLongOrNull(4), result.GetLongOrNull(5), result.GetIntOrNull(6), result.GetIntOrNull(7), isFile ? result.GetLongOrNull(8) : null);
                }
            }
        }

        void IMetaReader.Close()
        {
            Close();
        }

        public FolderMeta ReadNextFolder()
        {
            if (_folderMetaEnumerator.MoveNext())
            {
                return new FolderMeta()
                {
                    Id = (int)_folderMetaEnumerator.Current.Item1,
                    ParentId = (int?)_folderMetaEnumerator.Current.Item2,
                    Name = new StoredString(){StorageType = StringStorageType.LocalString, Value = _folderMetaEnumerator.Current.Item3},
                    Position = _folderMetaEnumerator.Current.Item1,
                    SurrogateId = _folderMetaEnumerator.Current.Item4 == null ? (Guid?)null : Guid.Parse(_folderMetaEnumerator.Current.Item4),
                    ModifiedDate = _folderMetaEnumerator.Current.Item5 == null ? (DateTimeOffset?)null : DateTimeOffset.FromUnixTimeMilliseconds(_folderMetaEnumerator.Current.Item5.Value),
                    PermissionMask = _folderMetaEnumerator.Current.Item6 == null ? (PermissionMask?)null : (PermissionMask)_folderMetaEnumerator.Current.Item6,
                    OwnerId = _folderMetaEnumerator.Current.Item7,
                    GroupId = _folderMetaEnumerator.Current.Item8
                };
            }

            IsFolderReaderAtEnd = true;
            return null;
        }

        public FileMeta ReadNextFile()
        {
            if (_fileMetaEnumerator.MoveNext())
            {
                if (_fileMetaEnumerator.Current.Item2 == null)
                {
                    throw new ApplicationException(
                        $"FileMeta {_fileMetaEnumerator.Current.Item1} has a null FolderId");
                }
                return new FileMeta()
                {
                    Id = (int)_fileMetaEnumerator.Current.Item1,
                    FolderId = (int)_fileMetaEnumerator.Current.Item2.Value,
                    Name = new StoredString(){StorageType = StringStorageType.LocalString, Value = _fileMetaEnumerator.Current.Item3},
                    Position = _fileMetaEnumerator.Current.Item1,
                    SurrogateId = _fileMetaEnumerator.Current.Item4 == null ? (Guid?)null : Guid.Parse(_fileMetaEnumerator.Current.Item4),
                    ModifiedDate = _fileMetaEnumerator.Current.Item5 == null ? (DateTimeOffset?)null : DateTimeOffset.FromUnixTimeMilliseconds(_fileMetaEnumerator.Current.Item5.Value),
                    PermissionMask = _fileMetaEnumerator.Current.Item6 == null ? (PermissionMask?)null : (PermissionMask)_fileMetaEnumerator.Current.Item6,
                    OwnerId = _fileMetaEnumerator.Current.Item7,
                    GroupId = _fileMetaEnumerator.Current.Item8,
                    FileLength = _fileMetaEnumerator.Current.Item9
                };
            }

            IsFileReaderAtEnd = true;
            return null;
        }

        public FileMeta ReadFileAt(long filePosition)
        {
            throw new NotImplementedException();
        }
    }
}