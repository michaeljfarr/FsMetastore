using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IndexedData;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.IO.Test;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    public class MetaStreamPersister : IMetaPersister
    {
        private readonly IScanDbBatchIOFactory _scanDbBatchIOFactory;
        private readonly IStringRefWriter _stringRefWriter;
        private readonly IMetaSerializer _metaSerializer;
        private readonly BatchStorageRules _batchStorageRules;
        private BinaryWriter _foldersWriter;
        private BinaryWriter _filesWriter;
        private IndexedDataFileWriter _indexedDataWriter;
        private readonly ITestOutputer _testOutput;

        private readonly List<FolderMeta> _folderMetas = new List<FolderMeta>();
        private readonly List<FileMeta> _fileMetas = new List<FileMeta>();
        
        public MetaStreamPersister(IScanDbBatchIOFactory scanDbBatchIOFactory, IStringRefWriter stringRefWriter, IMetaSerializer metaSerializer, BatchStorageRules batchStorageRules, ITestOutputer testOutput)
        {
            _scanDbBatchIOFactory = scanDbBatchIOFactory;
            _stringRefWriter = stringRefWriter;
            _metaSerializer = metaSerializer;
            _batchStorageRules = batchStorageRules;
            _testOutput = testOutput;
            _indexedDataWriter = new IndexedDataFileWriter(_scanDbBatchIOFactory);
            //_stringsWriter = _binaryWriterFactory.Create(MetaFileType.Strings);
        }
        
        private void CheckQueue<T>(List<T> metas, bool flush) where T:IItemMeta
        {
            var minAmount = 2200;
            var amountToFlush = 200;
            if (flush || _folderMetas.Count > minAmount)
            {
                var metasToPersist = flush ? metas : metas.Take(amountToFlush).ToList();
                if (metasToPersist.Any())
                {
                    metas.RemoveRange(0, metasToPersist.Count);
                    foreach (var metaToPersist in metasToPersist)
                    {
                        if (metaToPersist is FileMeta file)
                        {
                            _metaSerializer.WriteFile(_filesWriter, file, _batchStorageRules, _stringRefWriter);
                        }
                        else if (metaToPersist is FolderMeta folder)
                        {
                            _metaSerializer.WriteFolder(_foldersWriter, folder, _batchStorageRules, _stringRefWriter);
                        }
                        else
                        {
                            throw new ApplicationException($"unknown metadata type {metaToPersist?.GetType()}");
                        }
                    }
                }
            }
        }
        
        public void RevertNewFolders(long position)
        {
            while (_folderMetas.Any())
            {
                var last = _folderMetas.Last();
                _folderMetas.RemoveAt(_folderMetas.Count - 1);
                if (last.Id == position)
                {
                    return;
                }
            }
            _foldersWriter.BaseStream.Seek(position, SeekOrigin.Begin);
        }

        public void Open()
        {
            _testOutput.WriteLine($"Opening {nameof(MetaStreamPersister)}");
            _foldersWriter = _scanDbBatchIOFactory.CreateWriter(MetaFileType.Folders);
            _filesWriter = _scanDbBatchIOFactory.CreateWriter(MetaFileType.Files);
        }

        public void Close()
        {
            _testOutput.WriteLine($"Closing {nameof(MetaStreamPersister)}");
            CheckQueue(_folderMetas, true);
            CheckQueue(_fileMetas, true);
            
            if (_foldersWriter?.BaseStream?.Position != null &&
                _foldersWriter.BaseStream.Position != _foldersWriter.BaseStream.Length)
            {
                _foldersWriter.BaseStream.SetLength(_foldersWriter.BaseStream.Position);
            }

            _foldersWriter?.Close();
            _foldersWriter?.Dispose();
            _foldersWriter = null;
            _filesWriter?.Close();
            _filesWriter?.Dispose();
            _filesWriter = null;
            _indexedDataWriter?.Dispose();
            _indexedDataWriter = null;
        }

        public void Dispose()
        {
            Close();
        }

        /*
        * Machine Name
        * Date
        * Encoding (Utf8/Utf16)
        * Name Storage Strategy
            * Flexible (Each name is preceded by a byte that indicates the storage mode.)
            * Fixed (Every name is stored in the same way.)
        * Drives: 
            * Id: 
                * A Guid that defines the drive.
            * RootId: 
                * An n byte integer that represents the root folder of the drive.
            * MountPoint
                * A string indicating the mount point of the drive within the system.
         */

        public async Task<BatchSource> StoreSourceAsync(DriveMeta driveMeta, BatchSourceEncoding batchSourceEncoding,
            BatchInfo batchInfo, BatchStatistics batchStatistics, int scanGeneration)
        {
            var batchSource = BatchSource.FromDetails(driveMeta, batchSourceEncoding, batchInfo, _batchStorageRules); 
            await _scanDbBatchIOFactory.WriteJsonAsync(MetaFileType.Source, batchSource);
            return batchSource;
        }

        /*
        * Id: 
            * An n byte integer that represents the folder within this batch.
        * Name 
            * Potentially a "string ref", see notes below.
        * Mask: (A byte bitmask indicating which of the remaining values to expect)
        * SurrogateId: 
            * If the source file system knows it.
        * ParentId
        * Modified Date
        * Permission Mask
        * Owner Id
        * Group Id
         */
        public void StoreFolder(FolderMeta folderMeta)
        {
            folderMeta.Position = folderMeta.Id;
            _metaSerializer.WriteFolder(_foldersWriter, folderMeta, _batchStorageRules, _stringRefWriter);
        }

        public void StoreIndexedString(uint stringId, string val)
        {
            _indexedDataWriter.WriteStringItem(stringId, val);
        }

        public void StoreFile(FileMeta fileMeta)
        {
            _metaSerializer.WriteFile(_filesWriter, fileMeta, _batchStorageRules, _stringRefWriter);
        }
    }
}