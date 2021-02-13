using System.Text.Json.Serialization;

namespace FsMetastore.Model.StorageStrategy
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NameStorageStrategy
    {
        UnSet,
        /// <summary>
        /// StringRef uses a fixed length format where every string is stored separate file. Another requirement to meet
        /// fixed length is that the MetaValueMasks are specified globally instead of per record.
        /// </summary>
        StringRef,
        LocalString
    };
}