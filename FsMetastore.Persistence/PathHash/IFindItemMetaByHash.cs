using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.PathHash
{
    public interface IFindItemMetaByHash
    {
        IItemMeta CalculateFilePath(string path, bool caseInsensitive);
    }
}