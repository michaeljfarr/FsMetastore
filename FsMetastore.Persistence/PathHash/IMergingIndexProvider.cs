namespace FsMetastore.Persistence.PathHash
{
    public interface IMergingIndexProvider
    {
        /// <summary>
        /// If the item with the same is found in the existing data set, then its id will be returned.  Otherwise an unused
        /// id will allocated and be returned.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="caseInsensitive"></param>
        /// <returns></returns>
        long ExistingIndexOrNew(string path, bool caseInsensitive);
    }
}