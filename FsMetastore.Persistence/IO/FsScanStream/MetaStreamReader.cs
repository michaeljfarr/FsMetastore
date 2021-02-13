using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IO.FileBatches;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    /// <summary>
    /// MetaStreamReader assumes that file and folders are stored in separate file streams.  The actual serialization
    /// is delegated to IMetaSerializer, which can deserialize either protobuf or a custom format.
    /// </summary>
    class MetaStreamReader: IMetaReader
    {
        private readonly IScanDbBatchIOFactory _scanDbBatchIOFactory;
        private readonly BatchStorageRules _batchStorageRules;
        private readonly IMetaSerializer _metaSerializer;
        private BinaryReader _fileReader;
        private BinaryReader _folderReader;
        public MetaStreamReader(IScanDbBatchIOFactory scanDbBatchIOFactory, BatchStorageRules batchStorageRules, IMetaSerializer metaSerializer)
        {
            _scanDbBatchIOFactory = scanDbBatchIOFactory;
            _batchStorageRules = batchStorageRules;
            _metaSerializer = metaSerializer;
        }

        public bool Open(bool forRewrite = false)
        {
            _fileReader = _scanDbBatchIOFactory.CreateReader(MetaFileType.Files, forRewrite);
            _folderReader = _scanDbBatchIOFactory.CreateReader(MetaFileType.Folders, forRewrite);
            return _fileReader != null && _folderReader != null;
        }

        public void Close()
        {
            _fileReader?.Dispose();
            _fileReader = null;
            _folderReader?.Dispose();
            _folderReader = null;
        }

        public void Dispose()
        {
            Close();
        }


        public FolderMeta ReadNextFolder()
        {
            return _metaSerializer.ReadFolder(_folderReader, this._batchStorageRules);
        }

        public FileMeta ReadNextFile()
        {
            return _metaSerializer.ReadFile(_fileReader, this._batchStorageRules);
        }

        public bool IsFolderReaderAtEnd => _folderReader.BaseStream.Position >= _folderReader.BaseStream.Length;
        public bool IsFileReaderAtEnd => _fileReader.BaseStream.Position >= _fileReader.BaseStream.Length;


        public FileMeta ReadFileAt(long filePosition)
        {
            _fileReader.BaseStream.Seek(filePosition, SeekOrigin.Begin);
            return _metaSerializer.ReadFile(_fileReader, _batchStorageRules);
        }
    }
}