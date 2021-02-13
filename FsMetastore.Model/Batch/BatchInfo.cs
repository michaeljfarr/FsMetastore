namespace FsMetastore.Model.Batch
{
    public class BatchInfo
    {
        public int Generation { get; set; }
        public int NextFolderId { get; set; }
        public int NextFileId { get; set; }
    }
}