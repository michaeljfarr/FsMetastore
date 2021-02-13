using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.Zipper.Model
{
    public class MetaDbFileItem : IZipperPathItem   
    {
        public IItemMetaWithInfo FileInfo { get; set; }
        public string FullPath => FileInfo.FullPath;
    }
}