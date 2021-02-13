namespace FsMetastore.Persistence.IO.Commands
{
    public enum BatchCommandType
    {
        Unset, 
        WipeExisting,
        ThrowIfExists,
        ApplyDiffIfExists,
        ApplyDiff
    }
}