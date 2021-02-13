namespace FsMetastore.Persistence.Zipper.Model
{
    public enum NextItemFrom
    {
        Unset,
        /// <summary>
        /// If the current item is from the Baseline, it must have been deleted.
        /// </summary>
        Baseline,
        /// <summary>
        /// The current item is available in both sources and baseline, so was neither added nor deleted. 
        /// </summary>
        Both,
        /// <summary>
        /// The current item is only available from the source, so it must be a new item.
        /// </summary>
        Source,
        /// <summary>
        /// There are no remaining items.
        /// </summary>
        None
    }
}