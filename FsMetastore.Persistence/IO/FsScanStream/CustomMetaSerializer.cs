// using System;
// using System.IO;
// using FileMetaBatching.Model;
// using FileMetaBatching.Model.Batch;
// using FileMetaBatching.Model.Items;
// using FileMetaBatching.Model.StorageStrategy;
// using FileMetaBatching.Persistence.IndexedData;
//
// namespace FileMetaBatching.Persistence.IO.ScanDb
// {
//     public class CustomMetaSerializer : IMetaSerializer
//     {
//         public void WriteFile(BinaryWriter filesWriter, FileMeta fileMeta, BatchStorageRules batchStorageRules,
//             IStringRefWriter stringRefWriter)
//         {
//             filesWriter.Write(fileMeta.Id);
//             filesWriter.Write(fileMeta.FolderId);
//             WriteName(batchStorageRules, filesWriter, stringRefWriter, fileMeta.Name);
//
//             var fileMetaValueMask = batchStorageRules.FileMetaValueMask;
//             if (fileMetaValueMask == null)
//             {
//                 var dataMask = GetFileMetaValueMask(fileMeta);
//                 filesWriter.Write((byte) dataMask);
//                 fileMetaValueMask = dataMask;
//             }
//
//             if (fileMetaValueMask.Value.HasFlag(FileMetaValueMask.FileLength))
//                 filesWriter.Write(fileMeta.FileLength ?? 0);
//             if (fileMetaValueMask.Value.HasFlag(FileMetaValueMask.ModifiedDate))
//                 filesWriter.Write((fileMeta.ModifiedDate ?? DateTimeOffset.MinValue).ToUnixTimeMilliseconds());
//             if (fileMetaValueMask.Value.HasFlag(FileMetaValueMask.OwnerId))
//                 filesWriter.Write(fileMeta.OwnerId ?? 0);
//             if (fileMetaValueMask.Value.HasFlag(FileMetaValueMask.GroupId))
//                 filesWriter.Write(fileMeta.GroupId ?? 0);
//             if (fileMetaValueMask.Value.HasFlag(FileMetaValueMask.PermissionMask))
//                 filesWriter.Write((short) (fileMeta.PermissionMask ?? 0));
//             if (fileMetaValueMask.Value.HasFlag(FileMetaValueMask.SurrogateId))
//                 filesWriter.Write((fileMeta.SurrogateId ?? Guid.Empty).ToByteArray());
//         }
//         
//         private static void WriteName(BatchStorageRules batchStorageRules, BinaryWriter sourceWriter,
//             IStringRefWriter stringRefWriter, StoredString name)
//         {
//             var nameValue = name.Value;
//             if (batchStorageRules.NameStorageStrategy == NameStorageStrategy.LocalString)
//             {
//                 sourceWriter.Write(nameValue ?? "");
//             }
//             else if (batchStorageRules.NameStorageStrategy == NameStorageStrategy.StringRef)
//             {
//                 if (!name.Id.HasValue)
//                 {
//                     name.Id = stringRefWriter.AddString(nameValue);
//                     name.StorageType = StringStorageType.StringRef;
//                 }
//
//                 sourceWriter.Write(name.Id.Value);
//             }
//             else //if (_batchingConfiguration.NameStorageStrategy == NameStorageStrategy.PerItem)
//             {
//                 sourceWriter.Write((byte) name.StorageType);
//                 sourceWriter.Write(nameValue ?? "");
//             }
//         }
//         
//         private static FileMetaValueMask GetFileMetaValueMask(FileMeta fileMeta)
//         {
//             FileMetaValueMask fileMetaValueMask = 0;
//             if (fileMeta.FileLength.HasValue)
//             {
//                 fileMetaValueMask |= FileMetaValueMask.FileLength;
//             }
//
//             if (fileMeta.ModifiedDate.HasValue)
//             {
//                 fileMetaValueMask |= FileMetaValueMask.ModifiedDate;
//             }
//
//             if (fileMeta.OwnerId.HasValue)
//             {
//                 fileMetaValueMask |= FileMetaValueMask.OwnerId;
//             }
//
//             if (fileMeta.GroupId.HasValue)
//             {
//                 fileMetaValueMask |= FileMetaValueMask.GroupId;
//             }
//
//             if (fileMeta.PermissionMask.HasValue)
//             {
//                 fileMetaValueMask |= FileMetaValueMask.PermissionMask;
//             }
//
//             if (fileMeta.SurrogateId.HasValue)
//             {
//                 fileMetaValueMask |= FileMetaValueMask.SurrogateId;
//             }
//
//             return fileMetaValueMask;
//         }
//
//         public void WriteFolder(BinaryWriter _foldersWriter, FolderMeta folderMeta, BatchStorageRules batchStorageRules,
//             IStringRefWriter stringRefWriter)
//         {
//             folderMeta.Position = _foldersWriter.BaseStream.Position;
//             _foldersWriter.Write(folderMeta.Id);
//             _foldersWriter.Write(folderMeta.ParentId ?? 0);
//             WriteName(batchStorageRules, _foldersWriter, stringRefWriter, folderMeta.Name);
//
//             var folderMetaValueMask = batchStorageRules.FolderMetaValueMask;
//             if (folderMetaValueMask == null)
//             {
//                 var dataMask = GetFolderValueMask(folderMeta);
//
//                 _foldersWriter.Write((byte) dataMask);
//                 folderMetaValueMask = dataMask;
//             }
//
//             if (folderMetaValueMask.Value.HasFlag(FolderMetaValueMask.ModifiedDate))
//                 _foldersWriter.Write((folderMeta.ModifiedDate ?? DateTimeOffset.MinValue).ToUnixTimeMilliseconds());
//             if (folderMetaValueMask.Value.HasFlag(FolderMetaValueMask.OwnerId))
//                 _foldersWriter.Write(folderMeta.OwnerId ?? 0);
//             if (folderMetaValueMask.Value.HasFlag(FolderMetaValueMask.GroupId))
//                 _foldersWriter.Write(folderMeta.GroupId ?? 0);
//             if (folderMetaValueMask.Value.HasFlag(FolderMetaValueMask.PermissionMask))
//                 _foldersWriter.Write((short) (folderMeta.PermissionMask ?? 0));
//             if (folderMetaValueMask.Value.HasFlag(FolderMetaValueMask.SurrogateId))
//                 _foldersWriter.Write((folderMeta.SurrogateId ?? Guid.Empty).ToByteArray());
//         }
//         
//         private static FolderMetaValueMask GetFolderValueMask(FolderMeta folderMeta)
//         {
//             FolderMetaValueMask dataMask = 0;
//             if (folderMeta.ModifiedDate.HasValue)
//             {
//                 dataMask |= FolderMetaValueMask.ModifiedDate;
//             }
//
//             if (folderMeta.OwnerId.HasValue)
//             {
//                 dataMask |= FolderMetaValueMask.OwnerId;
//             }
//
//             if (folderMeta.GroupId.HasValue)
//             {
//                 dataMask |= FolderMetaValueMask.GroupId;
//             }
//
//             if (folderMeta.PermissionMask.HasValue)
//             {
//                 dataMask |= FolderMetaValueMask.PermissionMask;
//             }
//
//             if (folderMeta.SurrogateId.HasValue)
//             {
//                 dataMask |= FolderMetaValueMask.SurrogateId;
//             }
//
//             return dataMask;
//         }
//         public FileMeta ReadFile(BinaryReader _fileReader, BatchStorageRules batchStorageRules)
//         {
//             var pos = _fileReader.BaseStream.Position;
//             var fileMeta = new FileMeta
//             {
//                 Id = _fileReader.ReadInt32(),
//                 FolderId = _fileReader.ReadInt32(),
//                 Position = pos,
//             };
//             fileMeta.Name = ReadName(_fileReader, batchStorageRules);
//
//             var fileMetaValueMask = batchStorageRules.FileMetaValueMask ?? (FileMetaValueMask)_fileReader.ReadByte();
//             if(fileMetaValueMask.HasFlag(FileMetaValueMask.FileLength))
//             {
//                 fileMeta.FileLength = _fileReader.ReadInt64();
//             }
//             if(fileMetaValueMask.HasFlag(FileMetaValueMask.ModifiedDate))
//             {
//                 fileMeta.ModifiedDate = DateTimeOffset.FromUnixTimeMilliseconds(_fileReader.ReadInt64());
//             }
//             if(fileMetaValueMask.HasFlag(FileMetaValueMask.OwnerId))
//             {
//                 fileMeta.OwnerId = _fileReader.ReadInt32();
//             }
//             if(fileMetaValueMask.HasFlag(FileMetaValueMask.GroupId))
//             {
//                 fileMeta.GroupId = _fileReader.ReadInt32();
//             }
//             if(fileMetaValueMask.HasFlag(FileMetaValueMask.PermissionMask))
//             {
//                 fileMeta.PermissionMask = (PermissionMask) _fileReader.ReadInt16();
//             }
//             if(fileMetaValueMask.HasFlag(FileMetaValueMask.SurrogateId))
//             {
//                 fileMeta.SurrogateId = new Guid(_fileReader.ReadBytes(Constants.GuidBytes));
//             }
//
//             return fileMeta;
//         }
//         
//         public FolderMeta ReadFolder(BinaryReader _folderReader, BatchStorageRules batchStorageRules)
//         {
//             /*
// var folderMeta = _metaFactory.CreateFolder(directoryInfo, parent, surrogateId);
// _foldersWriter.Write(folderMeta.Id);
// _foldersWriter.Write((char)FolderMetaValueMask.ModifiedDate);
//
// if(folderMeta.PermissionMask.HasValue)
//     _foldersWriter.Write((short)folderMeta.PermissionMask.Value);
//
// if(folderMeta.Name.Id.HasValue)
//     _foldersWriter.Write(folderMeta.Name.Id.Value);
//
// WriteName(_batchingConfiguration, _foldersWriter, _stringRefWriter, folderMeta.Name);
// return folderMeta;
//              */
//             var pos = _folderReader.BaseStream.Position;
//
//             var folderMeta = new FolderMeta
//             {
//                 Id = _folderReader.ReadInt32(),
//                 ParentId = _folderReader.ReadInt32(),
//                 Position = pos
//             };
//             folderMeta.ParentId = folderMeta.ParentId == 0 ? null : folderMeta.ParentId;
//             folderMeta.Name = ReadName(_folderReader, batchStorageRules);
//
//             var folderMetaValueMask = batchStorageRules.FolderMetaValueMask ?? (FolderMetaValueMask)_folderReader.ReadByte();
//             if(folderMetaValueMask.HasFlag(FolderMetaValueMask.ModifiedDate))
//             {
//                 folderMeta.ModifiedDate = DateTimeOffset.FromUnixTimeMilliseconds(_folderReader.ReadInt64());
//             }
//             if(folderMetaValueMask.HasFlag(FolderMetaValueMask.OwnerId))
//             {
//                 folderMeta.OwnerId = _folderReader.ReadInt32();
//             }
//             if(folderMetaValueMask.HasFlag(FolderMetaValueMask.GroupId))
//             {
//                 folderMeta.GroupId = _folderReader.ReadInt32();
//             }
//             if(folderMetaValueMask.HasFlag(FolderMetaValueMask.PermissionMask))
//             {
//                 folderMeta.PermissionMask = (PermissionMask) _folderReader.ReadInt16();
//             }
//             if(folderMetaValueMask.HasFlag(FolderMetaValueMask.SurrogateId))
//             {
//                 folderMeta.SurrogateId = new Guid(_folderReader.ReadBytes(Constants.GuidBytes));
//             }
//
//             return folderMeta;
//         }
//         
//         private StoredString ReadName(BinaryReader reader, BatchStorageRules batchStorageRules)
//         {
//             
//             if (batchStorageRules.NameStorageStrategy == NameStorageStrategy.LocalString)
//             {
//                 return new StoredString()
//                 {
//                     Value = reader.ReadString()
//                 };
//             }
//             else if (batchStorageRules.NameStorageStrategy == NameStorageStrategy.StringRef)
//             {
//                 return new StoredString()
//                 {
//                     Id = reader.ReadInt32(),
//                     StorageType = StringStorageType.StringRef
//                 };
//             }
//             else //if (_batchingConfiguration.NameStorageStrategy == NameStorageStrategy.PerItem)
//             {
//                 var storedAs = (StringStorageType) reader.ReadByte();
//                 if (storedAs == StringStorageType.StringRef)
//                 {
//                     return new StoredString()
//                     {
//                         Id = reader.ReadInt32(),
//                         StorageType = StringStorageType.StringRef
//                     };
//                 }
//                 else
//                 {
//                     var itemVal = reader.ReadString();
//                     return new StoredString()
//                     {
//                         StorageType = storedAs,
//                         Value = itemVal
//                     };
//                 }
//             }
//         }
//         
//     }
// }