using FsMetastore.Model.Items;
using FsMetastore.Persistence.IndexedData;
using FsMetastore.Persistence.IOC;
using FsMetastore.Persistence.Meta;

namespace FsMetastore.Persistence.PathHash
{
    public class PathIndexCreator : IPathIndexCreator
    {
        private readonly IPathIndexWriter _pathIndexWriter;
        private readonly IPathHashCalculator _pathHashCalculator;
        private readonly IIndexedDataFileReader _indexedDataFileReader;

        public PathIndexCreator(
            IPathIndexWriter pathIndexWriter,
            IPathHashCalculator pathHashCalculator, 
            IIndexedDataFileReader indexedDataFileReader)
        {
            _pathIndexWriter = pathIndexWriter;
            _pathHashCalculator = pathHashCalculator;
            _indexedDataFileReader = indexedDataFileReader;
        }

        public void WritePathIndex(IMetaEnumerator metaEnumerator)
        {
            var hasIndexedNames = _indexedDataFileReader.OpenRead();
            _pathIndexWriter.Clean();
            _pathIndexWriter.InitDb();
            foreach (var info in metaEnumerator.ReadInfos())
            {
                if (hasIndexedNames)
                {
                    BatchScope.ReadName(_indexedDataFileReader, (ItemMetaWithInfo) info);
                }
                var infoHash = _pathHashCalculator.CalculatePathHash(info.FullPath, true);
                _pathIndexWriter.AddPathIndex(infoHash, info);
            }

            _pathIndexWriter.Flush();
        }
    }
}