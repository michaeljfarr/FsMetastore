using System.IO;

namespace FsMetastore.Persistence.Zipper.Model
{
    public class DirectoryZipperItem : IZipperPathItem
    {
        public DirectoryZipperItem(DirectoryInfo directoryInfo)
        {
            DirectoryInfo = directoryInfo;
        }

        public DirectoryInfo DirectoryInfo { get; }
        public string FullPath => DirectoryInfo.FullName;
    }
}