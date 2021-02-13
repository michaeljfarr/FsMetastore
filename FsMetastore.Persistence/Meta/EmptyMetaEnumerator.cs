using System.Collections.Generic;
using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.Meta
{
    class EmptyMetaEnumerator : IMetaEnumerator
    {
        public IEnumerable<IItemMetaWithInfo> ReadInfos()
        {
            return new IItemMetaWithInfo[0];
        }
    }
}