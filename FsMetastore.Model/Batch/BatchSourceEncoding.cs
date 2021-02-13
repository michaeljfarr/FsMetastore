using System.Text.Json.Serialization;

namespace FsMetastore.Model.Batch
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BatchSourceEncoding
    {
        UnSet,
        Utf8,
        Utf16
    };
}