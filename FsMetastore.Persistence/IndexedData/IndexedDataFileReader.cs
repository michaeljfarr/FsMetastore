using System;
using System.Collections.Generic;
using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IO.FsScanStream;

namespace FsMetastore.Persistence.IndexedData
{
    class IndexedDataFileReader : IIndexedDataFileReader
    {
        private readonly IScanDbBatchIOFactory _readerFactory;
        private BinaryReader _stringReader;
        private BinaryReader _indexReader;
        private long _indexFileLength;
        private long _numItems;
        private uint _itemSize = 8;
        //private uint _overflowItemSize = 12;
        private Dictionary<uint, int> _index = null;
        public IndexedDataFileReader(IScanDbBatchIOFactory readerFactory)
        {
            _readerFactory = readerFactory;
        }

        public bool OpenRead()
        {
            _stringReader = _readerFactory.CreateReader(MetaFileType.IndexedNames);
            if(_stringReader == null)
            {
                return false;
            }
            _indexReader = _readerFactory.CreateReader(MetaFileType.IndexedNamesIndex);
            _indexFileLength = _indexReader.BaseStream.Length;
            if(_indexFileLength % _itemSize != 0)
            {
                throw new ApplicationException($"Index size {_indexReader.BaseStream.Length} is malformed, not a multiple of {_itemSize}");
            }
            _numItems = _indexFileLength / _itemSize;
            if(_numItems<(5*1024*1024))
            {
                _index = new Dictionary<uint, int>();
                while (_indexReader.BaseStream.Position < _indexReader.BaseStream.Length)
                {
                    var id = _indexReader.ReadUInt32();
                    var position = _indexReader.ReadInt32();
                    _index[id] = position;
                }
            }
            else
            {
                _index = null;
            }

            return true;
        }

        private (uint id, long position) ReadIndexItemByIndex(long indexItemIndex)
        {
            _indexReader.BaseStream.Seek( indexItemIndex * _itemSize, SeekOrigin.Begin);
            var id = _indexReader.ReadUInt32();
            var position = _indexReader.ReadInt32();
            return (id, position);
        }

        private long FindItemPosition(uint itemId)
        {
            if(_index !=null)
            {
                return _index[itemId];
            }
            long l = 0, r = _numItems - 1; 
            while (l <= r) { 
                long m = l + (r - l) / 2; 
  
                // Check if x is present at mid 
                var item = ReadIndexItemByIndex(m);
                if (item.id == itemId) 
                    return item.position; 
  
                // If x greater, ignore left half 
                if (item.id < itemId) 
                    l = m + 1; 
  
                // If x is smaller, ignore right half 
                else
                    r = m - 1; 
            } 
  
            // if we reach here, then element was 
            // not present 
            return -1;
        }

        public string FindStringItem(uint itemId)
        {
            var pos = FindItemPosition(itemId);
            if(pos<0)
            {
                return null;
            }
            _stringReader.BaseStream.Seek(pos, SeekOrigin.Begin);
            var itemString = _stringReader.ReadString();
            return itemString;
        }

        public byte[] FindByteArrayItem(uint itemId)
        {
            var pos = FindItemPosition(itemId);
            _stringReader.BaseStream.Seek(pos, SeekOrigin.Begin);
            var stringLength = _indexReader.Read7BitEncodedInt();
            var itemString = _indexReader.ReadBytes(stringLength);
            return itemString;
        }

        

        public void Dispose()
        {
            _stringReader?.Dispose();
            _stringReader = null;
            _indexReader?.Dispose();
            _indexReader = null;
        }
    }
}