namespace FsMetastore.Persistence.Zipper.Model
{
    public interface IZipperPathItem : IZipperItem
    {
        string FullPath { get; }
    }
}