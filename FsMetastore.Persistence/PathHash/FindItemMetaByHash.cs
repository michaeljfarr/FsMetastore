using System;
using System.IO;
using System.Linq;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IO.FileBatches;

namespace FsMetastore.Persistence.PathHash
{
    class FindItemMetaByHash : IFindItemMetaByHash
    {
        private readonly IPathHashCalculator _pathHashCalculator;
        private readonly IPathIndexReader _pathIndexReader;
        private readonly IMetaReader _metaReader;

        public FindItemMetaByHash(IPathHashCalculator pathHashCalculator, IPathIndexReader pathIndexReader, IMetaReader metaReader)
        {
            _pathHashCalculator = pathHashCalculator;
            _pathIndexReader = pathIndexReader;
            _metaReader = metaReader;
        }

        public IItemMeta CalculateFilePath(string path, bool caseInsensitive)
        {
            var hash = _pathHashCalculator.CalculatePathHash(path, caseInsensitive);
            var filePositions = _pathIndexReader.ReadPotentialMetaPositions((ulong)hash).ToList();
            if(!filePositions.Any())
            {
                return null;
            }

            var fileName = Path.GetFileName(path);
            foreach(var filePosition in filePositions)
            {
                var meta = _metaReader.ReadFileAt(filePosition);
                if(meta == null)
                {
                    throw new ApplicationException($"Index is broken, missing object at {filePosition}");
                }
                if(string.Equals(meta.Name.Value, fileName, caseInsensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture ))
                {
                    return meta;
                }
            }
            return null;
        }
    }
}