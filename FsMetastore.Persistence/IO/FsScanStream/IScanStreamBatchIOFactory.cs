using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IndexedData;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    public interface IScanDbBatchIOFactory : IBatchIOFactory
    {
        void CleanPreOptimFiles();
        
        BinaryWriter CreateWriter(MetaFileType fileType);
        BinaryReader CreateReader(MetaFileType fileType, bool forRewrite = false);

        IIndexedDataFileReader CreateIndexedDataReader();
        IIndexedDataFileWriter CreateIndexedDataWriter();
    }
}