namespace FsMetastore.Persistence.IO.Change
{
    public enum ComparisonResult
    {
        Unset,
        FirstBeforeSecond,
        Same,
        SecondBeforeFirst
    };
}