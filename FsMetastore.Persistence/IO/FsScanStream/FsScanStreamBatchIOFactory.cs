using System;
using System.IO;
using System.Text;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IndexedData;
using FsMetastore.Persistence.IO.Test;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// All files are written to a single directory and share a single encoding type.
    /// </remarks>
    class FsScanStreamBatchIOFactory : BatchIOFactoryBase, IScanDbBatchIOFactory
    {
        private Encoding _encoding;

        public FsScanStreamBatchIOFactory(BatchIOConfig batchIOConfig, ITestOutputer testOutput) : base(batchIOConfig, testOutput)
        {
        }

        private Encoding EnsureEncoding()
        {
            if (_encoding == null)
            {

                _encoding = _batchIOConfig.BatchSourceEncoding == BatchSourceEncoding.Utf8 ? Encoding.UTF8 :
                    _batchIOConfig.BatchSourceEncoding == BatchSourceEncoding.Utf16 ? Encoding.Unicode :
                    throw new ApplicationException($"Unsupported encoding: {_batchIOConfig.BatchSourceEncoding}");
            }

            return _encoding;
        }

        public IIndexedDataFileWriter CreateIndexedDataWriter()
        {
            return new IndexedDataFileWriter(this);
        }

        public IIndexedDataFileReader CreateIndexedDataReader()
        {
            return new IndexedDataFileReader(this);
        }

        public BinaryWriter CreateWriter(MetaFileType fileType)
        {
            var fileStream = OpenFileStream(fileType, BatchFileNames.MetaStreamFileSuffix, FileMode.Create);
            return new BinaryWriter(fileStream, EnsureEncoding());
        }



        public BinaryReader CreateReader(MetaFileType fileType, bool forRewrite)
        {
            var filePath = CreateFilePath(fileType);
            if(!File.Exists(filePath))
            {
                return null;
            }

            if(forRewrite)
            {
                var newFilePath = CreatePreOptimFilePath(fileType);
                if(File.Exists(newFilePath))
                {
                    File.Delete(newFilePath);
                }
                File.Move(filePath, newFilePath);
                _testOutput?.WriteLine($"Opening Rewrite {FileMode.Open}:{filePath}");
                var fileStream = OpenFileWithWait(newFilePath, FileMode.Open, _batchIOConfig.MaxWait);
                return new BinaryReader(fileStream, EnsureEncoding());
            }
            else
            {
                _testOutput?.WriteLine($"Opening {FileMode.Open}: {filePath}");
                var fileStream = OpenFileWithWait(filePath, FileMode.Open, _batchIOConfig.MaxWait);
                return new BinaryReader(fileStream);
            }
        }

        private string CreatePreOptimFilePath(MetaFileType fileType)
        {
            return Path.Combine(_batchPathRoot, $"{fileType}{BatchFileNames.MetaStreamFileSuffix}.rx");
        }

        private string CreateFilePath(MetaFileType fileType)
        {
            return base.CreateFilePath(fileType, BatchFileNames.MetaStreamFileSuffix);
        }

        private void DeletePreOptimFile(MetaFileType fileType)
        {
            var newFilePath = CreatePreOptimFilePath(fileType);
            File.Delete(newFilePath);
        }

        public void CleanPreOptimFiles()
        {
            DeletePreOptimFile(MetaFileType.Folders);
            DeletePreOptimFile(MetaFileType.Files);
        }
    }
}