using FsMetastore.Persistence.Meta;

namespace FsMetastore.Persistence.PathHash
{
    public interface IPathIndexCreator
    {
        void WritePathIndex(IMetaEnumerator metaEnumerator);
    }
}