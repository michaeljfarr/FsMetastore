using System;

namespace FsMetastore.Model.Batch
{
    public class BatchIOConfig
    {
        public BatchSourceEncoding BatchSourceEncoding { get; set; }
        public string BatchPathRoot { get; set; }
        public string DiffPartRoot { get; set; }
        public TimeSpan MaxWait { get; set; } = TimeSpan.FromMinutes(5);
        /// <summary>
        /// The path on the file system where we want to collect file metadata from.  This may be null when doing
        /// an activity that doesn't involve reading from the filesystem such as exporting a diff from FileMetaDb or
        /// optimizing stringrefs.
        /// </summary>
        public string SourcePath { get; set; }
    }
}
