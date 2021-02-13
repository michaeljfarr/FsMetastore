using System.Collections.Generic;
using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.Meta
{
    /// <summary>
    /// IMetaEnumerator enumerates a stream of filesystem metadata and provides the full file path of each item. 
    /// </summary>
    public interface IMetaEnumerator
    {
        IEnumerable<IItemMetaWithInfo> ReadInfos();
    }
}