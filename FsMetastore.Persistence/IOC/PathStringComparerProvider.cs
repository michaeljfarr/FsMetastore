using System;

namespace FsMetastore.Persistence.IOC
{
    public class PathStringComparerProvider : IPathStringComparerProvider
    {
        public PathStringComparerProvider(StringComparer stringComparer)
        {
            StringComparer = stringComparer;
        }

        public static IPathStringComparerProvider MatchCase()
        {
            return new PathStringComparerProvider(StringComparer.Ordinal);
        }

        public StringComparer StringComparer { get; }
    }
}