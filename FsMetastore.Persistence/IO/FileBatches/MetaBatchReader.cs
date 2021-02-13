using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IO.FsScanStream;

namespace FsMetastore.Persistence.IO.FileBatches
{
    class MetaBatchReader : IMetaBatchReader
    {
        private readonly IScanDbBatchIOFactory _scanDbBatchIOFactory;
        public MetaBatchReader(IScanDbBatchIOFactory scanDbBatchIOFactory)
        {
            _scanDbBatchIOFactory = scanDbBatchIOFactory;
        }
        public Task<BatchSource> ReadSourceAsync()
        {
            return _scanDbBatchIOFactory.ReadJsonAsync<BatchSource>(MetaFileType.Source);
        }
    }
}