using System;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IO.FsMetaDb;
using FsMetastore.Persistence.IO.FsScanStream;
using FsMetastore.Persistence.IOC;

namespace FsMetastore.Persistence.IO.FileBatches
{
    /// <summary>
    /// MetaStreamPlusDbReader reads file and folders information from a database while applying changes from
    /// a file stream.
    /// </summary>
    class MetaStreamPlusDbReader : IMetaReader
    {
        private readonly MetaStreamReader _metaStreamReader;
        private readonly FsMetaDbMetaReader _fsMetaDbMetaReader;
        private readonly IBatchSourceProvider _batchSourceProvider;
        private FolderMeta _nextFolderFromImportDb = null;
        private FileMeta _nextFileFromImportDb = null;
        private FolderMeta _nextFolderFromScanDb = null;
        private FileMeta _nextFileFromScanDb = null;
        private readonly PathStack _importDbPathStack = new PathStack();
        private readonly PathStack _scanDbPathStack = new PathStack();

        public MetaStreamPlusDbReader(MetaStreamReader metaStreamReader, FsMetaDbMetaReader fsMetaDbMetaReader, IBatchSourceProvider batchSourceProvider)
        {
            _metaStreamReader = metaStreamReader;
            _fsMetaDbMetaReader = fsMetaDbMetaReader;
            _batchSourceProvider = batchSourceProvider;
            //todo: remove ugly side effect
            _fsMetaDbMetaReader.ReadFromDiff();
        }

        public void Dispose()
        {
            _metaStreamReader.Dispose();
            _fsMetaDbMetaReader.Dispose();
        }

        public bool IsFolderReaderAtEnd => _metaStreamReader.IsFolderReaderAtEnd && _fsMetaDbMetaReader.IsFolderReaderAtEnd;

        public bool IsFileReaderAtEnd => _metaStreamReader.IsFileReaderAtEnd && _fsMetaDbMetaReader.IsFileReaderAtEnd;

        public bool Open(bool forRewrite = false)
        {
            if (forRewrite == true)
            {
                throw new ArgumentException($"Rewrite is not supported for ScanDbPlusImportDbMetaReader");
            }
            _importDbPathStack.Clear();
            _scanDbPathStack.Clear();
            _nextFolderFromImportDb = null;
            _nextFileFromImportDb = null;
            _nextFolderFromScanDb = null;
            _nextFileFromScanDb = null;

            var opened = _metaStreamReader.Open() && _fsMetaDbMetaReader.Open();
            return opened;
        }

        public void Close()
        {
            _metaStreamReader.Close();
            _fsMetaDbMetaReader.Close();
        }

        private FolderMeta ReadNextFolder(IMetaReader metaReader, PathStack pathStack)
        {
            var nextFolder = metaReader.IsFolderReaderAtEnd ? null : metaReader.ReadNextFolder();
            if (nextFolder != null)
            {
                pathStack.NextItem(nextFolder.Id, nextFolder.ParentId, nextFolder.Name.Value);
            }

            return nextFolder;
        }

        /// <summary>
        /// Reads the folders out while preserving the alphabetical ordering of the paths
        /// </summary>
        public FolderMeta ReadNextFolder()
        {
            var scanDbFolder = _nextFolderFromScanDb ?? ReadNextFolder(_metaStreamReader, _scanDbPathStack);
            var importDbFolder = _nextFolderFromImportDb ?? ReadNextFolder(_fsMetaDbMetaReader, _importDbPathStack);
            if (importDbFolder == null && scanDbFolder == null)
            {
                //no objects left
                return null;
            }
            if (importDbFolder == null)
            {
                //we are at the end of the import, but there are still some items in the ScanDb
                _nextFolderFromScanDb = null;
                return scanDbFolder;
            }
            if (scanDbFolder == null)
            {
                //we are at the end of the scandb, but there are still some items in the ImportDb
                _nextFolderFromImportDb = null;
                return importDbFolder;
            }

            // if (scanDbFolder.Id == importDbFolder.Id)
            // {
            //     if (scanDbFolder.Name.Value != importDbFolder.Name.Value)
            //     {
            //         throw new ApplicationException($"ScanDb {scanDbFolder.Id}/{scanDbFolder.Name.Value} doesn't match ImportDb  {importDbFolder.Id}/{importDbFolder.Name.Value}");
            //     }
            //     _nextFolderFromScanDb = null;
            //     _nextFolderFromImportDb = null;
            //     //if there is an option, always use the ImportDb which will have the latest variant
            //     return importDbFolder;
            // }

            var scanDbPath = _scanDbPathStack.GetPath(null);
            var importDbPath = _importDbPathStack.GetPath(null);

            var comparison = _batchSourceProvider.StringComparer.Compare(scanDbPath, importDbPath);
            if (comparison == 0)//file exists in both places 
            {
                _nextFolderFromScanDb = null;
                _nextFolderFromImportDb = null;
                //if there is an option, always use the ImportDb which will have the latest variant
                return importDbFolder;
            }
            else if (comparison < 0)//folder only exists within ScanDb
            {
                _nextFolderFromScanDb = null;
                //we didn't read this folder yet, so save it for next time
                _nextFolderFromImportDb = importDbFolder;
                return scanDbFolder;
            }
            else //comparison > 0 folder only exists within ImportDb
            {
                _nextFolderFromImportDb = null;
                //we didn't read this folder yet, so save it for next time
                _nextFolderFromScanDb = scanDbFolder;
                return importDbFolder;
            }
        }

        /// <summary>
        /// Todo: this function sucks - plz fix.  Same with folder one.
        /// </summary>
        /// <returns></returns>
        public FileMeta ReadNextFile()
        {
            var scanDbFile = _nextFileFromScanDb ?? (_metaStreamReader.IsFileReaderAtEnd ? null : _metaStreamReader.ReadNextFile());
            var importDbFile = _nextFileFromImportDb ?? (_fsMetaDbMetaReader.IsFileReaderAtEnd ? null : _fsMetaDbMetaReader.ReadNextFile());
            if (scanDbFile?.FolderId != _scanDbPathStack.CurrentFolderId)
            {
                _nextFileFromScanDb = scanDbFile; 
                scanDbFile = null;
            }
            if (importDbFile?.FolderId != _importDbPathStack.CurrentFolderId)
            {
                _nextFileFromImportDb = importDbFile; 
                importDbFile = null;
            }

            if (importDbFile == null && scanDbFile == null)
            {
                return null;
            }

            if (importDbFile == null)
            {
                //we are at the end of the import, but there are still some items in the ScanDb
                _nextFileFromScanDb = null;
                return scanDbFile;
            }
            if (scanDbFile == null)
            {
                //we are at the end of the scandb, but there are still some items in the ImportDb
                _nextFileFromImportDb = null;
                return importDbFile;
            }

            // if (scanDbFile.Id == importDbFile.Id)
            // {
            //     _nextFileFromScanDb = null;
            //     _nextFileFromImportDb = null;
            //     return importDbFile;
            // }

            var scanDbPath = _scanDbPathStack.GetPath(scanDbFile.Name.Value);
            var importDbPath = _importDbPathStack.GetPath(importDbFile.Name.Value);
            var comparison = _batchSourceProvider.StringComparer.Compare(scanDbPath, importDbPath);
            if (comparison == 0)
            {
                _nextFileFromScanDb = null;
                _nextFileFromImportDb = null;
                return importDbFile;
            }
            else if (comparison < 0)//file only exists within scandb
            {
                _nextFileFromScanDb = null;
                //we didn't read this folder yet, so save it for next time
                _nextFileFromImportDb = importDbFile;
                return scanDbFile;
            }
            else //comparison > 0 //file only exists within importdb
            {
                _nextFileFromImportDb = null;
                //we didn't read this folder yet, so save it for next time
                _nextFileFromScanDb = scanDbFile;
                return importDbFile;
            }
        }

        public FileMeta ReadFileAt(long filePosition)
        {
            throw new NotImplementedException("ReadFileAt not implemented for ScanDbPlusImportDbMetaReader");
            //return _scanDbMetaReader.ReadFileAt(filePosition);
        }
    }
}