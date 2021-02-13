using System.Threading.Tasks;
using FsMetastore.Model.Batch;

namespace FsMetastore.Persistence.IO.Commands
{
    public interface IBatchCommandService
    {
        Task<(BatchSource batchSource, BatchStatistics batchStatistics)> CreateImportDb(string dbFolder,
            string sourcePath, BatchCommandType batchCommandType);

        Task ExportChangesAsMetaStream(string dbFolder, string exportFolder, int generation);
        Task ImportChangesFromMetaStream(string dbFolder, string diffDbFolderToImport);
    }
}