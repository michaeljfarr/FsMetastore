using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.Sqlite;

namespace FsMetastore.Persistence.IO.FsMetaDb
{
    class FsMetaDbContext : FsMetaDbSqliteBase, IFsMetaDbContext
    {
        private readonly IMetaBatchReader _metaBatchReader;

        public FsMetaDbContext(BatchIOConfig batchIOConfig, IMetaBatchReader metaBatchReader) : base(batchIOConfig)
        {
            _metaBatchReader = metaBatchReader;
        }

        public async Task<BatchSource> ReadSourceAsync()
        {
            return await _metaBatchReader.ReadSourceAsync();
        }

        public int? ExecuteScalarSqlInt32(string commandText)
        {
            EnsureConnectionOpen();
            return base.ExecuteScalarInt32(commandText);
        }

        public IEnumerable<(int, string)> SelectListOfInt32String(string commandText)
        {
            EnsureConnectionOpen();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = commandText;
                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {
                    yield return (result.GetInt32(0), result.GetString(1));
                }
            }
        }

        public IEnumerable<FolderMeta> FoldersFromGen(int generation)
        {
            EnsureConnectionOpen();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"select Id, ParentId, Name, SurrogateId, ModifiedDate, PermissionMask, OwnerId, GroupId from FolderMeta where ModifiedGeneration={generation} order by Ord";
                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {
                    yield return new FolderMeta {
                        Id = result.GetInt32(0),
                        ParentId = result.GetIntOrNull(1),
                        Name = new StoredString(){
                            StorageType = StringStorageType.LocalString,
                            Value = result[2] as string
                        },
                        SurrogateId = result.GetGuidOrNull(3),
                        ModifiedDate = result.GetDateTimeOffsetOrNull(4),
                        PermissionMask = result.GetEnumMaskOrNull<PermissionMask>(5),
                        OwnerId = result.GetIntOrNull(6),
                        GroupId = result.GetIntOrNull(7),
                    };
                }
            }
        }

        public IEnumerable<FileMeta> FilesFromGen(int generation)
        {
            EnsureConnectionOpen();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"select f.Id, f.ParentId, f.Name, f.SurrogateId, f.ModifiedDate, f.PermissionMask, f.OwnerId, f.GroupId, f.FileLength from FileMeta f LEFT JOIN FolderMeta fld on fld.Id=f.ParentId where f.ModifiedGeneration={generation} order by fld.Ord, f.Name";
                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {
                    yield return new FileMeta {
                        Id = result.GetInt32(0),
                        FolderId = result.GetInt32(1),
                        Name = new StoredString(){
                            StorageType = StringStorageType.LocalString,
                            Value = result[2] as string
                        },
                        SurrogateId = result.GetGuidOrNull(3),
                        ModifiedDate = result.GetDateTimeOffsetOrNull(4),
                        PermissionMask = result.GetEnumMaskOrNull<PermissionMask>(5),
                        OwnerId = result.GetIntOrNull(6),
                        GroupId = result.GetIntOrNull(7),
                        FileLength = result.GetIntOrNull(8), 
                    };
                }
            }
        }

        public void GetSource()
        {
            throw new NotImplementedException();
        }

        private void EnsureConnectionOpen()
        {
            if (_connection == null || _connection.State == ConnectionState.Closed)
            {
                base.OpenConnection();
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}