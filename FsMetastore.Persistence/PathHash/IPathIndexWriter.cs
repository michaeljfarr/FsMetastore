using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.PathHash
{
    public interface IPathIndexWriter
    {
        void AddPathIndex(ulong pathHash, IItemMetaWithInfo info);
        void Flush();
        void Clean();
        void InitDb();
    }
}