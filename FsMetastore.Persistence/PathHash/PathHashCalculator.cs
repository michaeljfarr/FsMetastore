using System;
using System.Data.HashFunction;
using System.IO;

namespace FsMetastore.Persistence.PathHash
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// By default, I dont think we need a secure hash here.  CRC will smaller than a secure hash, but probably should check performance
    /// Assuming we aim for less than 99.9% chance of collisions in a data set of 10M records, then 64bit is enough and 32bit is not enough.
    ///     https://preshing.com/20110504/hash-collision-probabilities/
    /// See the https://github.com/rurban/smhasher to understand basic quality check for hashes and relative performance.  We also need
    /// platform independence, so these are the main candidates:
    ///   Metrohash64, City64, Spooky64, MurmurHash3
    /// This talks up City64 ... https://web.stanford.edu/class/ee380/Abstracts/121017-slides.pdf
    /// Initial implementation selected from https://github.com/brandondahler/Data.HashFunction/
    /// </remarks>
    class PathHashCalculator : IPathHashCalculator
    {
        private readonly IHashFunction _hash;

        public PathHashCalculator(IHashFunction hash)
        {
            _hash = hash;
            if(_hash.HashSizeInBits!=64)
            {
                throw new ArgumentOutOfRangeException(nameof(_hash), $"hash must be 64 bit, was {_hash.HashSizeInBits}bit");
            }
        }

        public ulong CalculatePathHash(string path, bool caseInsensitive)
        {
            path = Path.GetFullPath(path);
            if(path.EndsWith("/"))
            {
                throw new ApplicationException($"Nomalized path ends with / ({path})");
            }
            if(caseInsensitive)
            {
                path = path.ToLowerInvariant();
            }

            path = path.Replace('\\', '/');
            var hash = _hash.ComputeHash(path).Hash;
            var val = (ulong)BitConverter.ToInt64(hash, 0);
            return val;
        }
    }
}
