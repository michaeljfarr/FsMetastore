using System.Text.Json.Serialization;

namespace FsMetastore.Model.Batch
{
    /// <summary>
    /// StorageType identifies the primary persistence system.  Also consider BatchStorageRules to determine the  
    /// storage structure within batch. 
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StorageType
    {
        Unset,
        NoopTimer,
        MetaStream,
        FileMetaDb,
        MetaStreamPlusDb
    }
}