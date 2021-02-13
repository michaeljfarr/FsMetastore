using System.Collections.Generic;

namespace FsMetastore.Persistence.IO.Change
{
    public static class PathComparator
    {
         public static ComparisonResult CompareFilePaths(string first, string second, IComparer<string> stringComparer)
         {
             //this assumes ascii based file system. 
             if (first.Length > 1 && first.EndsWith("\\"))
             {
                 first = first.Substring(0, first.Length - 1);
             }

             var first1 = first.Replace('/', (char) 1).Replace('\\', (char) 1);
             var second1 = second.Replace('/', (char) 1).Replace('\\', (char) 1);
             var comparison = stringComparer.Compare(first1, second1);
             return comparison == 0 ? 
                 ComparisonResult.Same : 
                 comparison<0 ? ComparisonResult.FirstBeforeSecond : ComparisonResult.SecondBeforeFirst;
         }
    }
}