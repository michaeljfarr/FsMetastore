namespace FsMetastore.Model.Batch
{
    public static class BatchFileNames
    {
        public const string JsonSuffix = ".json";
        public const string MetaStreamFileSuffix = ".fmb";
        public static string CreateFileName(MetaFileType fileType, string suffix)
        {
            return $"{fileType}{suffix}";
        }
        public static string[] MetaStreamFileNames()
        {
            return new[]{ $"{MetaFileType.Files}{MetaStreamFileSuffix}",  $"{MetaFileType.Folders}{MetaStreamFileSuffix}"};
        }

        public static readonly string FileMetaDbFileName = $"{MetaFileType.FileMetaDb}.db";
    }
}