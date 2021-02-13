using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.IO.FsScanStream;
using FsMetastore.Persistence.IO.Test;
using FsMetastore.Persistence.IOC;
using FsMetastore.Persistence.Sqlite;
using Microsoft.Data.Sqlite;

namespace FsMetastore.Persistence.IO.FsMetaDb
{
    class FsMetaDbPersister : FsMetaDbSqliteBase, IMetaPersister, IOrderableHierarchy
    {
        private readonly IScanDbBatchIOFactory _scanDbBatchIOFactory;
        private readonly BatchStorageRules _batchStorageRules;
        private readonly IBatchSourceProvider _batchSourceProvider;
        private readonly ITestOutputer _testOutputer;
        private int _currentGeneration;

        private readonly List<FolderMeta> _folderMetas = new List<FolderMeta>();
        private readonly List<FileMeta> _fileMetas = new List<FileMeta>();

        public FsMetaDbPersister(IScanDbBatchIOFactory scanDbBatchIOFactory, 
            BatchIOConfig batchIOConfig, 
            BatchStorageRules batchStorageRules,
            IBatchSourceProvider batchSourceProvider,
            ITestOutputer testOutputer): base(batchIOConfig)
        {
            _scanDbBatchIOFactory = scanDbBatchIOFactory;
            _batchStorageRules = batchStorageRules;
            _batchSourceProvider = batchSourceProvider;
            _testOutputer = testOutputer;
        }
        
        public void Dispose()
        {
            Close();
        }

        public override void Flush()
        {
            if (_connection.State == ConnectionState.Open)
            {
                CheckQueue(FolderMetaTableName, _folderMetas, true);
                CheckQueue(FileMetaTableName, _fileMetas, true);
                if (_currentGeneration > 1)
                {
                    var orderer = new HierarchyOrderer(this, _testOutputer, _currentGeneration, _batchSourceProvider.StringComparer);
                    orderer.UpdateOrds();
                }
            }
        }
        
        public void Open()
        {
            OpenConnection();
        }
        
        private void EnsureTablesExists(bool caseInsensitive)
        {
            var collate = caseInsensitive ? "collate nocase" : "collate binary"; 
            base.EnsureTableExists(JsonTableName, "(Name Text PRIMARY KEY, Json Text)");
            //FolderMeta a = null;
            base.EnsureTableExists(FolderMetaTableName, $"(Id INTEGER PRIMARY KEY, ParentId INTEGER, Ord INTEGER, Name {collate}, SurrogateId Text, ModifiedDate INTEGER, PermissionMask INTEGER, OwnerId INTEGER, GroupId INTEGER, CreatedGeneration INTEGER, ModifiedGeneration INTEGER)");
            //FileMeta b = null;
            base.EnsureTableExists(FileMetaTableName, $"(Id INTEGER PRIMARY KEY, ParentId INTEGER, Name {collate}, FileLength INTEGER, SurrogateId Text, ModifiedDate INTEGER, PermissionMask INTEGER, OwnerId INTEGER, GroupId INTEGER, CreatedGeneration INTEGER, ModifiedGeneration INTEGER)");
            base.EnsureIndex(true, "Folder_ParentName_UK", FolderMetaTableName, "(ParentId, Name, Id, Ord)");
            base.EnsureIndex(true, "File_ParentName_UK", FileMetaTableName, "(ParentId, Name)");
            
            base.EnsureTableExists(SourceTableName, $"(Id COLLATE NOCASE PRIMARY KEY, MachineName COLLATE NOCASE, MountPoint {collate}, PathCaseRule COLLATE NOCASE, FolderMetaValueMask INTEGER, FileMetaValueMask INTEGER)");
            base.EnsureTableExists(GenerationTableName, $"(Id INTEGER PRIMARY KEY, Started INTEGER NOT NULL, Completed INTEGER, NumFoldersFound INTEGER, NumFilesFound INTEGER, NumFileChanges INTEGER)");
        }

        IEnumerable<(int id, string name, ulong? currentOrd)> IOrderableHierarchy.GetChildren(int? parentId)
        {
            using (var command = _connection.CreateCommand())
            {
                if (parentId != null)
                {
                    command.CommandText = $"Select Id, Name, Ord from {FolderMetaTableName} where ParentId={parentId}";
                }
                else
                {
                    command.CommandText = $"Select Id, Name, Ord from {FolderMetaTableName} where ParentId is null";
                }

                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {                   //id 1                   parent id  2             name    3            surrogate  4         modified date 5            permissionmask 6           ownerid 7                groupid 8
                    yield return (result.GetInt32(0), result[1] as string, result.GetULongOrNull(2));
                }
            }
        }

        SqliteTransaction IOrderableHierarchy.StartTransaction()
        {
            var transaction = _connection.BeginTransaction();
            return transaction;
        }
        void IOrderableHierarchy.CommitTransaction(SqliteTransaction transaction)
        {
            transaction.Commit();
        }

        void IOrderableHierarchy.UpdateOrds(List<(int id, ulong expectedOrd)> ordsThatNeedUpdating)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"UPDATE {FolderMetaTableName} SET Ord=$ord where Id=$id";
                command.AddNamedParameter("$ord");
                command.AddNamedParameter("$id");
                foreach (var ordThatNeedUpdating in ordsThatNeedUpdating)
                {
                    command.SetNamedParameter("$ord", ordThatNeedUpdating.expectedOrd);
                    command.SetNamedParameter("$id", ordThatNeedUpdating.id);
                    var result = command.ExecuteNonQuery();
                }
            }
        }

        public async Task<BatchSource> StoreSourceAsync(DriveMeta driveMeta, BatchSourceEncoding batchSourceEncoding,
            BatchInfo batchInfo, BatchStatistics batchStatistics, int scanGeneration)
        {
            EnsureTablesExists(BatchSourceProvider.StoreAsCaseInsensitive(driveMeta));
            //var maxGeneration = ExecuteScalarInt32($"select max(Generation) from (select max(ModifiedGeneration) Generation from FileMeta union select max(ModifiedGeneration) Generation from FolderMeta) {FolderMetaTableName}") ?? 0;
            _currentGeneration = scanGeneration;
            
            var batchSource = BatchSource.FromDetails(driveMeta, batchSourceEncoding, batchInfo, _batchStorageRules);
            batchSource.BatchInfo.Generation = _currentGeneration;

            //This is essentially an unnecessary duplicate of the json data, but it sometimes saves a few
            //keystrokes to have it in sql 
            await EnsureSourceData(driveMeta.Id, batchSource.MachineName, driveMeta.MountPoint, driveMeta.PathCaseRule,
                _batchStorageRules.FolderMetaValueMask, _batchStorageRules.FileMetaValueMask);
            
            //this is also currently unused by the api. 
            await WriteGenerationData(_currentGeneration, batchStatistics);
            
            await UpsertJson("Source", System.Text.Json.JsonSerializer.Serialize(batchSource));

            //also write the Source data as json. 
            await _scanDbBatchIOFactory.WriteJsonAsync(MetaFileType.Source, batchSource);
            return batchSource;
        }

        private async Task UpsertJson(string name, string jsonString)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"REPLACE INTO {JsonTableName}(Name, Json) VALUES($name, $val)";

                command.AddNamedParameter("$name", name);
                command.AddNamedParameter("$val", jsonString);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task WriteGenerationData(int currentGeneration, BatchStatistics batchStatistics)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText =  $"INSERT INTO {GenerationTableName}(Id, Started) VALUES ($Id, $currentDate)" +
                   $"ON CONFLICT(Id) DO UPDATE SET Completed=$currentDate, NumFoldersFound=$NumFoldersFound, NumFilesFound=$NumFilesFound, NumFileChanges=$NumFileChanges WHERE Id=$Id";

                command.AddNamedParameter("$Id", currentGeneration);
                command.AddNamedParameter("$currentDate", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                command.AddNamedParameter("$NumFoldersFound", batchStatistics?.NumFoldersFound ?? 0);
                command.AddNamedParameter("$NumFilesFound", batchStatistics?.NumFilesFound ?? 0);
                command.AddNamedParameter("$NumFileChanges", batchStatistics?.NumFileChanges ?? 0);
                
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task EnsureSourceData(Guid driveMetaId, string machineName, string mountPoint, PathCaseRule pathCaseRule, FolderMetaValueMask? folderMetaValueMask, FileMetaValueMask? fileMetaValueMask)
        {
            //EnsureTableExists(SourceTableName, $"(Id COLLATE NOCASE PRIMARY KEY, MachineName COLLATE NOCASE, MountPoint {collate}, PathCollation COLLATE NOCASE, FolderMetaValueMask INTEGER, FileMetaValueMask INTEGER)");
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"REPLACE INTO {SourceTableName}(Id, MachineName, MountPoint, PathCaseRule, FolderMetaValueMask, FileMetaValueMask) VALUES ($driveMetaId, $machineName, $mountPoint, $pathCaseRule, $folderMetaValueMask, $fileMetaValueMask)";
                
                command.AddNamedParameter("$driveMetaId", driveMetaId.ToString());
                command.AddNamedParameter("$machineName", machineName);
                command.AddNamedParameter("$mountPoint", mountPoint);
                command.AddNamedParameter("$pathCaseRule", pathCaseRule.ToString());
                command.AddNamedParameter("$folderMetaValueMask", folderMetaValueMask);
                command.AddNamedParameter("$fileMetaValueMask", fileMetaValueMask);
                await command.ExecuteNonQueryAsync();
            }
        }

        public void StoreFolder(FolderMeta folderMeta)
        {
            folderMeta.Position = folderMeta.Id;
            _folderMetas.Add(folderMeta);
            CheckQueue(FolderMetaTableName, _folderMetas, false);
        }

        private void CheckQueue<T>(string metaTableName, List<T> metas, bool flush) where T:IItemMeta
        {
            var minAmount = 2200;
            var amountToFlush = 200;
            if (flush || _folderMetas.Count > minAmount)
            {
                var metasToPersist = flush ? metas : metas.Take(amountToFlush).ToList();
                if (metasToPersist.Any())
                {
                    if (!flush)
                    {
                        metas.RemoveRange(0, metasToPersist.Count);
                    }

                    using (var command = CreateCommand(metaTableName))
                    {
                        using (command.Transaction = _connection.BeginTransaction())
                        {
                            foreach (var metaToPersist in metasToPersist)
                            {
                                StoreMeta(command, metaToPersist);
                            }
                            command.Transaction.Commit();
                        }
                    }
                    
                    if (flush)
                    {
                        metas.Clear();
                    }
                }
            }
        }

        private void StoreMeta(string tableName, IItemMeta folderMeta)
        {
            using (var command = CreateCommand(tableName))
            {
                StoreMeta(command, folderMeta);
            }
        }

        private void StoreMeta(SqliteCommand command, IItemMeta folderMeta)
        {
            var folder = folderMeta as FolderMeta;
            var file = folderMeta as FileMeta;

            var name = folder != null ? folder.Name.Value : file.Name.Value;
            command.SetNamedParameter("$Id", folderMeta.Id);
            command.SetNamedParameter("$ParentId", folderMeta.ParentId);
            command.SetNamedParameter("$Name", name);
            if (file != null)
            {
                command.SetNamedParameter("$FileLength", file.FileLength);
            }

            command.SetNamedParameter("$ModifiedDate", folderMeta.ModifiedDate?.ToUnixTimeMilliseconds());
            command.SetNamedParameter("$PermissionMask", folderMeta.PermissionMask);
            command.SetNamedParameter("$OwnerId", folderMeta.OwnerId);
            command.SetNamedParameter("$GroupId", folderMeta.GroupId);
            command.SetNamedParameter("$Generation", _currentGeneration);
            if (file == null && _currentGeneration == 1)
            {
                command.SetNamedParameter("$Ord", folderMeta.Id * HierarchyOrderer.OrdOffset(_currentGeneration));
            }

            command.ExecuteNonQuery();
        }
        

        private SqliteCommand CreateCommand(string tableName)
        {
            var command = _connection.CreateCommand();
            var isFile = tableName == FileMetaTableName;

            var insertOrd = _currentGeneration == 1;
            var lengthField = isFile ? ", FileLength" : insertOrd ? ", Ord" : "";
            var lengthValue = isFile ? ", $FileLength" : insertOrd ? ", $Ord" : "";

            command.CommandText =
                $"INSERT INTO {tableName}(Id, ParentId, Name{lengthField}, ModifiedDate, PermissionMask, OwnerId, GroupId, CreatedGeneration, ModifiedGeneration) VALUES ($Id, $ParentId, $Name{lengthValue}, $ModifiedDate, $PermissionMask, $OwnerId, $GroupId, $Generation, $Generation)" +
                $"ON CONFLICT(Id) DO UPDATE SET ModifiedDate=$ModifiedDate, PermissionMask=$PermissionMask, OwnerId=$OwnerId, GroupId=$GroupId, ModifiedGeneration=$Generation WHERE Id=$Id";

            command.AddNamedParameter("$Id");
            command.AddNamedParameter("$ParentId");
            command.AddNamedParameter("$Name");
            if (isFile)
            {
                command.AddNamedParameter("$FileLength");
            }
            else if(insertOrd)
            {
                command.AddNamedParameter("$Ord");
            }

            command.AddNamedParameter("$ModifiedDate");
            command.AddNamedParameter("$PermissionMask");
            command.AddNamedParameter("$OwnerId");
            command.AddNamedParameter("$GroupId");
            command.AddNamedParameter("$Generation");
            return command;
        }



        
        public void StoreFile(FileMeta fileMeta)
        {
            _fileMetas.Add(fileMeta);
            CheckQueue(FileMetaTableName, _fileMetas, false);
        }


        void IMetaPersister.Close()
        {
            base.Close();
        }

        /// <summary>
        /// For ImportDb, we need to remove records added in this generation.
        /// </summary>
        /// <param name="position"></param>
        /// <remarks>
        /// Working out what to delete is a bit complicated, so here is a worked example.
        /// Two folders found in first scan, Folder A (Id=5) containing B (Id=6) and Folder C (Id=7) Containing D (Id=8) 
        /// In generation 2, folder Z with id=50 was added to Folder A
        /// In generation 3:
        ///     folder A still didn't change, so RevertFolderPosition(5) was called.
        ///         folder Z (id=50) must not be deleted
        ///         folder A (id=5) must not be deleted
        ///         folder B (id=6) must be removed (was temporarily inserted with CreatedGeneration=CurrentGeneration)
        ///     folder C still didn't change, so RevertFolderPosition(7) was called.
        ///         folder D (id=8) must be deleted
        ///         folder C (id=7) must be deleted
        /// The rule is
        ///     DELETE WHERE Id>={position} && CreatedGeneration={CurrentGeneration}
        /// </remarks>
        public void RevertNewFolders(long position)
        {
            //so for ImportDb we just delete records if we added them in this generation
            //AddGeneration, UpdatedGeneration
            while (_folderMetas.Any())
            {
                var last = _folderMetas.Last();
                _folderMetas.RemoveAt(_folderMetas.Count - 1);
                if (last.Id == position)
                {
                    return;
                }
            }
            //there were so many folders that we have already flushed them.
            var query = $"Delete from {FolderMetaTableName} where CreatedGeneration={_currentGeneration} AND Id>={position}";
            var itemsDeleted = base.Execute(query);
        }
    }
}