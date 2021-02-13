using System;
using System.Collections.Generic;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IO.FileBatches;

namespace FsMetastore.Persistence.IO.FsMetaDb
{
    public interface IFsMetaDbContext : IMetaBatchReader, IDisposable
    {
        int? ExecuteScalarSqlInt32(string commandText);
        IEnumerable<(int, string)> SelectListOfInt32String(string commandText);
        IEnumerable<FolderMeta> FoldersFromGen(int generation);
        IEnumerable<FileMeta> FilesFromGen(int generation);
    }
}