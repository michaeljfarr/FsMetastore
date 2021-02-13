using System.Collections.Generic;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IOC;
using FsMetastore.Persistence.Meta;
using FsMetastore.Persistence.Zipper.Model;

namespace FsMetastore.Persistence.Zipper
{
    class FileSystemZipEnumerableFactory
    {
        private readonly BatchIOConfig _batchIOConfig;
        private readonly IMetaFactory _metaFactory;
        private readonly IPathStringComparerProvider _pathStringComparerProvider;

        public FileSystemZipEnumerableFactory(BatchIOConfig batchIOConfig, IMetaFactory metaFactory, IPathStringComparerProvider pathStringComparerProvider)
        {
            _batchIOConfig = batchIOConfig;
            _metaFactory = metaFactory;
            _pathStringComparerProvider = pathStringComparerProvider;
        }

        public IEnumerable<IZipperItem> Open()
        {
            var enumerator = new FileSystemZipEnumerable(_batchIOConfig.SourcePath, _metaFactory,
                _pathStringComparerProvider.StringComparer);
            return enumerator;
        }
    }
}