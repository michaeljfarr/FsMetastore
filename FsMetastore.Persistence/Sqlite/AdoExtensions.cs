using System;
using System.Data;
using System.Diagnostics;

namespace FsMetastore.Persistence.Sqlite
{
    static class AdoExtensions
    {
        public static ulong? GetULongOrNull(this IDataReader reader, int i)
        {
            if(reader.IsDBNull(i))
            {
                return null;
            }
            else
            {
                try
                {
                    return (ulong) reader.GetInt64(i);
                }
                catch
                {
#if DEBUG
                    Debugger.Launch();             
#endif
                    throw;
                }
            }

        }

        public static int? GetIntOrNull(this IDataReader reader, int i)
        {
            if(reader.IsDBNull(i))
            {
                return null;
            }
            else
            {
                return reader.GetInt32(i);
            }
        }
        
        public static long? GetLongOrNull(this IDataReader reader, int i)
        {
            return reader.IsDBNull(i) ? (long?)null : reader.GetInt64(i);
        }
        
        public static T? GetEnumMaskOrNull<T>(this IDataReader reader, int i) where T: struct
        {
            return reader.IsDBNull(i) ? (T?)null : (T)(object)reader.GetInt32(i);
        }
        
        public static DateTimeOffset? GetDateTimeOffsetOrNull(this IDataReader reader, int i)
        {
            return reader.IsDBNull(i) ? (DateTimeOffset?)null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(i));
        }
        public static Guid? GetGuidOrNull(this IDataReader reader, int i)
        {
            return reader.IsDBNull(i) ? (Guid?)null : reader.GetGuid(i);
        }
    }
}