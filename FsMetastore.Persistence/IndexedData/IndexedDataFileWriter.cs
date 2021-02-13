using System;
using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IO.FsScanStream;

namespace FsMetastore.Persistence.IndexedData
{
    public class IndexedDataFileWriter : IIndexedDataFileWriter, IDisposable
    {
        private readonly IScanDbBatchIOFactory _writerFactory;
        private BinaryWriter _dataWriter;
        private BinaryWriter _indexWriter;
        private uint? _lastItemId = null;

        public IndexedDataFileWriter(IScanDbBatchIOFactory writerFactory)
        {
            _writerFactory = writerFactory;
        }

        public void Create()
        {
            _dataWriter = _writerFactory.CreateWriter(MetaFileType.IndexedNames);
            _indexWriter = _writerFactory.CreateWriter(MetaFileType.IndexedNamesIndex);
        }

        public void WriteStringItem(uint itemId, string itemValue)
        {
            if(_lastItemId > itemId)
            {
                throw new ApplicationException("ItemIds must be in increasing order");
            }
            if(_dataWriter.BaseStream.Position>Int32.MaxValue)
            {
                //the idea would be to simply overflow into a new file
                //  -- either a single massive file with 8byte position or
                //  -- multiple < 1GB file.
                throw new NotSupportedException($"Currently we do not support more than {Int32.MaxValue/1024}kb of indexed data.");
            }
            var pos = (int)_dataWriter.BaseStream.Position;
            _indexWriter.Write(itemId);
            _indexWriter.Write(pos);
            _dataWriter.Write(itemValue);
        }

        public void WriteByteArrayItem(uint itemId, byte[] itemData)
        {
            if(_lastItemId > itemId)
            {
                throw new ApplicationException("ItemIds must be in increasing order");
            }
            var pos = _dataWriter.BaseStream.Position;
            _indexWriter.Write(itemId);
            _indexWriter.Write(pos);
            _dataWriter.Write7BitEncodedInt(itemData.Length);
            _dataWriter.Write(itemData);
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            _dataWriter?.Close();
            _dataWriter?.Dispose();
            _dataWriter = null;
            _indexWriter?.Close();
            _indexWriter?.Dispose();
            _indexWriter = null;
        }
    }
}
