using System;

namespace FsMetastore.Model.Items.FileMetaDb
{
    public class Generation
    {
        public int Id { get; set; }
        public DateTimeOffset Started { get; set; }
        public DateTimeOffset Completed { get; set; }
        public int NumFoldersFound { get; set; }
        public int NumFilesFound { get; set; }
        public int NumFileChanges { get; set; }
    }
}