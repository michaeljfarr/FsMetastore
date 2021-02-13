using System.Collections.Generic;

namespace FsMetastore.Persistence.IndexedData
{
    public class NoopStringRefWriter : IStringRefWriter
    {
        private int _maxId;

        public void InitDb()
        {
            
        }

        public void DeleteDb()
        {
            
        }

        public int AddString(string newString)
        {
            return ++_maxId;
        }

        public IEnumerable<(long currentId, long targetId, string val)> GetStringMap()
        {
            yield break;
        }

        public IEnumerable<(long stringId, string val)> GetUniqueStrings()
        {
            yield break;
        }

        public void Flush()
        {
        }

        public void Close()
        {
        }
    }
}