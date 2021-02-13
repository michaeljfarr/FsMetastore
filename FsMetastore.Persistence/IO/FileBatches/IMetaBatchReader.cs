using System.Threading.Tasks;
using FsMetastore.Model.Batch;

namespace FsMetastore.Persistence.IO.FileBatches
{
    public interface IMetaBatchReader
    {
        Task<BatchSource> ReadSourceAsync();
    }
}