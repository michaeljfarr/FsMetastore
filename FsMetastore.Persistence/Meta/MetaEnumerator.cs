using System;
using System.Collections.Generic;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.Crawler;
using FsMetastore.Persistence.IO.FileBatches;

namespace FsMetastore.Persistence.Meta
{
    class MetaEnumerator : IMetaEnumerator
    {
        private readonly IMetaReader _metaReader;

        public MetaEnumerator(IMetaReader metaReader)
        {
            _metaReader = metaReader;
        }

        public IEnumerable<IItemMetaWithInfo> ReadInfos()
        {
            var folderStack = new FolderStack();
            if (!_metaReader.Open())
            {
                throw new ApplicationException("Failed to open ScanDb.");
            }
            var rootFolder = _metaReader.ReadNextFolder();
            if(rootFolder == null)
            {
                if (_metaReader.ReadNextFile()!=null || !_metaReader.IsFileReaderAtEnd)
                {
                    throw new ApplicationException($"Root Folder not found in");
                }

                yield break;
            }
            
            yield return folderStack.Follow(rootFolder);
            var nextFile= _metaReader.ReadNextFile();
            while (!_metaReader.IsFolderReaderAtEnd)
            {
                nextFile = nextFile ?? _metaReader.ReadNextFile();
                //read all the files at this point of the hierarchy
                while(nextFile?.ParentId == folderStack.Current.Id)
                {
                    yield return new ItemMetaWithInfo(nextFile, folderStack.Current);
                    nextFile = _metaReader.IsFileReaderAtEnd ? null : _metaReader.ReadNextFile();
                }
                var folder = _metaReader.IsFolderReaderAtEnd ? null : _metaReader.ReadNextFolder();
                if (folder != null)
                {
                    yield return folderStack.Follow(folder);
                }

                Console.WriteLine(folderStack.Current.FullPath);
            }

            if (!_metaReader.IsFileReaderAtEnd)
            {
                nextFile = nextFile ?? _metaReader.ReadNextFile();
            }

            while(nextFile?.ParentId == folderStack.Current.Id)
            {
                yield return new ItemMetaWithInfo(nextFile, folderStack.Current);
                nextFile = _metaReader.IsFileReaderAtEnd ? null : _metaReader.ReadNextFile();
            }
            if(!_metaReader.IsFileReaderAtEnd)
            {
                throw new ApplicationException($"Files not matched in {folderStack?.Current?.FullPath}");
            }
        }
    }
}