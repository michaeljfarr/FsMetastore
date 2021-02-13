using System;
using System.Collections.Generic;
using System.IO;

namespace FsMetastore.Tests.TestHelpers
{
    public class FileChangeCreator
    {
        private readonly string _basePath;
        private readonly List<(string subPath, bool isFolder)> _elements = new List<(string subPath, bool isFolder)>();

        public FileChangeCreator(): this(TestHelpers.RecreateTestSubFolder($"Changes")) 
        {
        }

        public FileChangeCreator(string basePath)
        {
            RejectRootFilesystem(basePath);
            _basePath = basePath;
        }

        public FileChangeCreator AddElement(string subPath, bool isFolder)
        {
            RejectRootFilesystem(subPath);

            _elements.Add((subPath, isFolder));
            return this;
        }

        private static void RejectRootFilesystem(string subPath)
        {
            if (subPath.StartsWith("..") || (subPath.Length<6 && (subPath.StartsWith("/") || subPath.StartsWith("\\") || (subPath.Length > 1 && subPath[1] == ':'))))
            {
                throw new ApplicationException($"Lets not accidentally delete things in our root file system {subPath}.");
            }
        }

        public void Remove()
        {
            Directory.Delete(_basePath, true);
        }

        public void CreateAll()
        {
            foreach (var element in _elements)
            {
                var path = Path.Combine(_basePath, element.subPath);
                if (element.isFolder)
                {
                    Directory.CreateDirectory(path);
                }
                else
                {
                    EnsureFolder(Path.GetDirectoryName(path));
                    File.WriteAllText(path, "some stuff");
                }
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        public void Copy(string from, string toFolder)
        {
            EnsureFolder(toFolder);
            foreach (var fromItem in Directory.EnumerateFileSystemEntries(from))
            {
                var relative = Path.GetRelativePath(from, fromItem);
                var target = Path.Combine(toFolder, relative);
                if (File.Exists(fromItem))
                {
                    EnsureFolder(Path.GetDirectoryName(target));
                    File.Copy(fromItem, target);
                }
            }
        }
    }
}