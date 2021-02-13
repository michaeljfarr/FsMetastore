using System.Collections.Generic;

namespace FsMetastore.Persistence.IndexedData
{
    public interface IStringRefWriter
    {
        void InitDb();
        void DeleteDb();

        int AddString(string newString);
        IEnumerable<(long currentId, long targetId, string val)> GetStringMap();
        IEnumerable<(long stringId, string val)> GetUniqueStrings();
        void Flush();
        void Close();
    }
}