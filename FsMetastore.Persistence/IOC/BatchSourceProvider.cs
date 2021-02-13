using System;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.IOC
{
    public class BatchSourceProvider: IBatchSourceProvider
    {
        public BatchSource BatchSource { get; set; }
        public StringComparer StringComparer => GetStringComparer();

        private StringComparer GetStringComparer()
        {
            if (BatchSource == null)
            {
                throw new ApplicationException("BatchSourceProvider not initialized");
            }

            if (BatchSource.Drive == null || BatchSource.Drive.PathCaseRule == PathCaseRule.Unset)
            {
                throw new ApplicationException("BatchSourceProvider.Drive.PathCaseRule not initialized");
            }
            
            if(StoreAsCaseInsensitive(BatchSource.Drive))
            {
                return StringComparer.OrdinalIgnoreCase;
            }
            if(BatchSource.Drive.PathCaseRule == PathCaseRule.Sensitive ||
               BatchSource.Drive.PathCaseRule == PathCaseRule.Ntfs)
            {
                return StringComparer.Ordinal;
            }
            throw new ApplicationException($"PathCaseRule ({BatchSource.Drive.PathCaseRule}) not recognized");
        }

        public static bool StoreAsCaseInsensitive(DriveMeta batchSourceDrive)
        {
            return batchSourceDrive.PathCaseRule == PathCaseRule.Preserving ||
                   batchSourceDrive.PathCaseRule == PathCaseRule.Insensitive;
        }
    }
}