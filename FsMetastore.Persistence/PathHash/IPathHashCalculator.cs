namespace FsMetastore.Persistence.PathHash
{
    public interface IPathHashCalculator
    {
        ulong CalculatePathHash(string path, bool caseInsensitive);
    }
}