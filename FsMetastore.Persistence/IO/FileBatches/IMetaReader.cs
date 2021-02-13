using System;
using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.IO.FileBatches
{
    /// <summary>
    /// IMetaReader is used by MetaEnumerator to read through a filesystem data from a number of different topologies.
    /// (These topologies include databases and multiple file-streams).  The requirements for an IMetaReader
    /// implementation are:
    /// 1. Files and Folders are read in lexicographical order of their full file path.  This is the same order that
    /// would be returned by recursing through the file system. 
    ///
    /// While all implementations of IMetaReader currently rely on separate storage of files and folders, it would be
    /// simple to build a stream that stored files and folders in a single stream as implemented by
    /// FileSystemMetaReader.  
    /// 
    /// This approach does introduce a little bit of complexity without a direct practical payoff.  However, it enables 
    /// us to compare multiple topologies an so understand what works best in each situation from an overall performance
    /// and memory efficiency point of view. 
    /// </summary>
    public interface IMetaReader : IDisposable
    {
        bool IsFolderReaderAtEnd { get; }
        bool IsFileReaderAtEnd { get; }
        bool Open(bool forRewrite = false);
        void Close();
        FolderMeta ReadNextFolder();
        FileMeta ReadNextFile();
        FileMeta ReadFileAt(long filePosition);
    }
}