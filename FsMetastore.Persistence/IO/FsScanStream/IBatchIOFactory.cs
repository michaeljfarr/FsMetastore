using System.Threading.Tasks;
using FsMetastore.Model.Batch;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    public interface IBatchIOFactory
    {
        Task<T> ReadJsonAsync<T>(MetaFileType fileType);
        Task WriteJsonAsync<T>(MetaFileType fileType, T objectToWrite);
    }
}