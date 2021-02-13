using System.Text.Json.Serialization;
using FsMetastore.Model.StorageStrategy;

namespace FsMetastore.Model.Items
{
    public class StoredString
    {
        /// <summary>
        /// This is the method for storing the string
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StringStorageType StorageType { get; set; }
        /// <summary>
        /// If the string is stored by reference, then this is the reference id.
        /// </summary>
        public int? Id { get; set; }
        /// <summary>
        /// This is the actual string value.
        /// </summary>
        public string Value { get; set; }
    }
}