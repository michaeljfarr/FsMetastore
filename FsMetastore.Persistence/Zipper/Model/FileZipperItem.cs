using System.IO;

namespace FsMetastore.Persistence.Zipper.Model
{
    /// <summary>
    /// These are produced by FileSystemMetaReader2
    /// </summary>
    public class FileZipperItem : IZipperPathItem   
    {
        public FileZipperItem(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }

        public FileInfo FileInfo { get; }
        public string FullPath => FileInfo.FullName;
    }
}