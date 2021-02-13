using System.Text.Json.Serialization;

namespace FsMetastore.Model.Batch
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PathCaseRule
    {
        Unset,
        /// <summary>
        /// The only truly case insensitive volume type seems to be FAT32
        /// </summary>
        Insensitive,
        /// <summary>
        /// NTFS is treated in the same way as Sensitive to allow for the small number of scenarios where it is. 
        /// </summary>
        /// <remarks>
        /// Although we dont currently handle the potential for case sensitivity variation within NTFS,
        /// we may do in the future. 
        /// 
        /// NTFS is typically case-preserving and case-insensitive.  However, Windows does allow case sensitive
        /// filenames on NTFS and controls this at a per directory level; the most common place to find case
        /// sensitive folders is in WSL.
        /// https://devblogs.microsoft.com/commandline/per-directory-case-sensitivity-and-wsl/
        /// </remarks>
        Ntfs,
        /// <summary>
        /// When reading a file called "FILE", the system may return a file called "file" if it exists.  Files are
        /// stored with Preserved case, but with NOCASE collation enabled.
        /// </summary>
        /// <remarks>
        /// This covers OSX in case insensitive mode where lookups are case insensitive but storage isn't.
        /// </remarks>
        Preserving,
        /// <summary>
        /// The differences btw Preserving and Sensitive are
        ///  - whether a folder name case change results in a delete and a create or just a change in name
        ///  - the db collation used by sqlite.
        /// </summary>
        /// <remarks>
        /// This includes any bsd like file system that allows the separate folder/filename to be stored where the
        /// name is the same but with different case/accents.
        ///  
        /// The encoding of the filenames (latin1, utf8, utf16 etc) is handled in the spider and is irrelevant
        /// to us after that.  We always order file based on character ordinal from a storage point of view.
        /// The downstream systems are responsible for character collation rules 
        /// </remarks>
        Sensitive
    }
}