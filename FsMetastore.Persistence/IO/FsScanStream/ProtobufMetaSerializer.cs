using System;
using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Model.Protobuf;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IndexedData;
using ProtoBuf;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    public class ProtobufMetaSerializer : IMetaSerializer
    {
        public void WriteFile(BinaryWriter writer, FileMeta fileMeta, BatchStorageRules batchStorageRules,
            IStringRefWriter stringRefWriter)
        {
            WriteFile(writer.BaseStream, fileMeta);
        }

        public void WriteFolder(BinaryWriter writer, FolderMeta folderMeta, BatchStorageRules batchStorageRules,
            IStringRefWriter stringRefWriter)
        {
            WriteFolder(writer.BaseStream, folderMeta);
        }

        public IItemMeta ReadItem(BinaryReader reader)
        {
            return ReadItem(reader.BaseStream);
        }

        public FolderMeta ReadFolder(BinaryReader reader, BatchStorageRules batchStorageRules)
        {
            var item = ReadItem(reader.BaseStream);
            if (item == null)
            {
                return null;
            }

            if (item is FolderMeta folder)
            {
                return folder;
            }
            else
            {
                throw new ApplicationException($"Item read from stream was not {nameof(FolderMeta)}");
            }
        }

        public FileMeta ReadFile(BinaryReader reader, BatchStorageRules batchStorageRules)
        {
            var item = ReadItem(reader.BaseStream);
            if (item == null)
            {
                return null;
            }

            if (item is FileMeta file)
            {
                return file;
            }
            else
            {
                throw new ApplicationException($"Item read from stream was not {nameof(FileMeta)}");
            }
        }


        public void WriteFile(Stream stream, FileMeta fileMeta)
        {
            if (fileMeta.FileLength == null)
            {
                throw new ArgumentException("fileMeta.FileLength is null");
            }

            var protoMeta = new ProtoMeta()
            {
                Id = fileMeta.Id,
                ParentId = fileMeta.FolderId,
                Name = fileMeta.Name.Value,
                FileLength = fileMeta.FileLength.Value,
                GroupId = fileMeta.GroupId,
                OwnerId = fileMeta.OwnerId,
                SurrogateId = fileMeta.SurrogateId,
                ModifiedDate = fileMeta.ModifiedDate,
                PermissionMask = fileMeta.PermissionMask
            };
            Serializer.SerializeWithLengthPrefix(stream, protoMeta, PrefixStyle.Base128);
        }

        public void WriteFolder(Stream stream, FolderMeta folderMeta)
        {
            folderMeta.Position = stream.Position;
            var protoMeta = new ProtoMeta()
            {
                Id = folderMeta.Id,
                ParentId = folderMeta.ParentId,
                Name = folderMeta.Name.Value,
                GroupId = folderMeta.GroupId,
                OwnerId = folderMeta.OwnerId,
                SurrogateId = folderMeta.SurrogateId,
                ModifiedDate = folderMeta.ModifiedDate,
                PermissionMask = folderMeta.PermissionMask
            };
            Serializer.SerializeWithLengthPrefix(stream, protoMeta, PrefixStyle.Base128);
        }


        public IItemMeta ReadItem(Stream stream)
        {
            var pos = stream.Position;
            var meta = Serializer.DeserializeWithLengthPrefix<ProtoMeta>(stream, PrefixStyle.Base128);
            if (meta == null)
            {
                return null;
            }

            if (meta.FileLength == null)
            {
                return new FolderMeta
                {
                    Id = meta.Id,
                    ParentId = meta.ParentId,
                    Name = new StoredString()
                    {
                        StorageType = StringStorageType.LocalString,
                        Value = meta.Name
                    },
                    GroupId = meta.GroupId,
                    OwnerId = meta.OwnerId,
                    SurrogateId = meta.SurrogateId,
                    ModifiedDate = meta.ModifiedDate,
                    PermissionMask = meta.PermissionMask,
                    Position = pos
                };
            }
            else
            {
                if (meta.ParentId == null)
                {
                    throw new ArgumentNullException("meta.ParentId");
                }

                return new FileMeta
                {
                    Id = meta.Id,
                    FolderId = meta.ParentId.Value,
                    Name = new StoredString()
                    {
                        StorageType = StringStorageType.LocalString,
                        Value = meta.Name
                    },
                    FileLength = meta.FileLength,
                    GroupId = meta.GroupId,
                    OwnerId = meta.OwnerId,
                    SurrogateId = meta.SurrogateId,
                    ModifiedDate = meta.ModifiedDate,
                    PermissionMask = meta.PermissionMask,
                    Position = pos
                };
            }
        }
    }
}