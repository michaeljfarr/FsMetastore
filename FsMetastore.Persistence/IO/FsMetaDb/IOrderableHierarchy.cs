using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace FsMetastore.Persistence.IO.FsMetaDb
{
    public interface IOrderableHierarchy
    {
        IEnumerable<(int id, string name, ulong? currentOrd)> GetChildren(int? parentId);
        void UpdateOrds(List<(int id, ulong expectedOrd)> ordsThatNeedUpdating);
        SqliteTransaction StartTransaction();
        void CommitTransaction(SqliteTransaction transaction);
    }
}