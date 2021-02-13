using System;
using FsMetastore.Model.Items;
using ProtoBuf;
using ProtoBuf.Meta;

namespace FsMetastore.Model.Protobuf
{
    [ProtoContract]
    public class DateTimeOffsetSurrogate
    {
        [ProtoMember(1)]
        public long UnixTimeMilliseconds { get; set; }

        public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset value)
        {
            return new DateTimeOffsetSurrogate {UnixTimeMilliseconds = value.ToUnixTimeMilliseconds()};
        }

        public static implicit operator DateTimeOffset(DateTimeOffsetSurrogate value)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(value.UnixTimeMilliseconds);
        }

        private static bool _registered = false;
        private static object LockObject = new object();
        public static void Register()
        {
            lock (LockObject)
            {
                if (_registered) return;
                RuntimeTypeModel.Default.Add(typeof(DateTimeOffset), false)
                    .SetSurrogate(typeof(DateTimeOffsetSurrogate));
                _registered = true;
            }
        }
    }
    
    [ProtoContract]
    public class ProtoMeta
    {
        [ProtoMember(1)]
        public int Id { get; set; } 

        [ProtoMember(2)]
        public int? ParentId { get; set; }
        
        [ProtoMember(3)]
        public string Name { get; set; }

        [ProtoMember(5)]
        public DateTimeOffset? ModifiedDate { get; set; }
        
        [ProtoMember(6)]
        public PermissionMask? PermissionMask { get; set; }
        
        /// <summary>
        /// Length will be null for folders and non-null for files
        /// </summary>
        [ProtoMember(7)]
        public long? FileLength { get; set; }
        
        [ProtoMember(8)]
        public Guid? SurrogateId { get; set; }
        
        [ProtoMember(9)]
        public int? OwnerId { get; set; }
        
        [ProtoMember(10)]
        public int? GroupId { get; set; }
    }
}