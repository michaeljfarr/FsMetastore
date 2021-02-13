using System.Collections.Generic;

namespace FsMetastore.Persistence.PathHash
{
    public interface IPathIndexReader
    {
        /// <summary>
        /// Return the number of items with a matching hash, typically there will be only 1 item.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        IEnumerable<long> ReadPotentialFolderPositions(ulong hash);
        IEnumerable<long> ReadPotentialMetaPositions(ulong hash);
    }
}