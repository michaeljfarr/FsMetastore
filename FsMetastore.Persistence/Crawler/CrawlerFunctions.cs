using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FsMetastore.Persistence.Crawler
{
    static class CrawlerFunctions
    {
        public static IEnumerable<FileInfo> SafeEnumerateFiles(DirectoryInfo pathInfo, StringComparer stringComparer)
        {
            try
            {
                return pathInfo.EnumerateFiles().OrderBy(a=>a.Name, stringComparer);
            }
            catch (Exception)
            {
                return new FileInfo[0];
            }
        }

        public static IEnumerable<DirectoryInfo> SafeEnumerateDirectories(DirectoryInfo pathInfo, StringComparer stringComparer)
        {
            try
            {
                return pathInfo.EnumerateDirectories().OrderBy(a=>a.Name, stringComparer);
            }
            catch (Exception)
            {
                return new DirectoryInfo[0];
            }
        }

        public static IEnumerable<FileSystemInfo> SafeEnumerateFileSystemInfos(DirectoryInfo pathInfo, StringComparer stringComparer)
        {
            try
            {
                return pathInfo.EnumerateFileSystemInfos().OrderBy(a=>a.Name, stringComparer);
            }
            catch (Exception)
            {
                return new FileSystemInfo[0];
            }
        }
    }
}