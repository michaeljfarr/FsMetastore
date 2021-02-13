namespace FsMetastore.Persistence.Enumeration
{
    public interface IPutBackEnumerator<out T> where T : class
    {
        bool MoveNext();
        void Reset();
        void PutBack();
        T Current { get; }
    }
}