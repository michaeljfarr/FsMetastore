using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.Crawler;
using FsMetastore.Persistence.Enumeration;
using FsMetastore.Persistence.Meta;
using FsMetastore.Persistence.Zipper.Model;

namespace FsMetastore.Persistence.Zipper
{
    /// <summary>
    /// Creates an enumerator of file system items (wrapped as IZipperItem) for a given path and
    ///  - alphabetically sorts the file hierarchy
    ///  - discards permission exceptions.
    ///  - retrieves consistent Guid from root folder (this feature is incomplete).
    ///  - returns wrapped DriveInfo as first item, then parent folder prior to the first item
    /// </summary>
    /// <remarks>
    /// The implementation isn't particularly efficient.  Ideally we could wrap the low level libraries in a more
    /// efficient way, perhaps looking at FileSystemEnumerator.Windows and other variations for an approach.  
    /// </remarks>
    class FileSystemZipEnumerable : IEnumerable<IZipperItem>
    {
        private readonly string _sourcePath;
        private readonly IMetaFactory _metaFactory;
        private readonly StringComparer _pathStringComparer;

        public FileSystemZipEnumerable(string sourcePath, IMetaFactory metaFactory, StringComparer pathStringComparer)
        {
            _sourcePath = sourcePath;
            _metaFactory = metaFactory;
            _pathStringComparer = pathStringComparer;
        }

        private IEnumerator<IZipperItem> Open(string sourcePath)
        {
            var directory = new DirectoryInfo(sourcePath);
            if (!directory.Exists)
            {
                throw new ApplicationException($"Unknown path: {sourcePath}");
            }
            var ancestorDirectoryInfo = new List<IZipperItem>();
            do
            {
                ancestorDirectoryInfo.Add(new DirectoryZipperItem(directory));
                directory = directory.Parent;
            } while (directory != null);

            var (driveInfo, driveMeta) = ReadDriveMetaFromDisk(((DirectoryZipperItem)ancestorDirectoryInfo.Last()).DirectoryInfo);
            IZipperItem drive = new DriveZipperItem(driveInfo, driveMeta);
            ancestorDirectoryInfo.Add(drive);
            ancestorDirectoryInfo.Reverse();
            
            var newItemEnumerator = RecurseItems(new DirectoryInfo(sourcePath));
            var itemEnumerator = new AppendEnumerable<IZipperItem>(ancestorDirectoryInfo, newItemEnumerator).GetEnumerator();
            return itemEnumerator;
        }
        
        public IEnumerator<IZipperItem> GetEnumerator()
        {
            return Open(_sourcePath);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerable<IZipperItem> RecurseItems(DirectoryInfo curDirectory)
        {
            foreach (var file in CrawlerFunctions.SafeEnumerateFiles(curDirectory, _pathStringComparer))
            {
                yield return new FileZipperItem(file);
            }
            
            //let caller know that we read the last file.
            yield return null;

            foreach (var dir in CrawlerFunctions.SafeEnumerateDirectories(curDirectory, _pathStringComparer))
            {
                yield return new DirectoryZipperItem(dir);
                foreach (var item in RecurseItems(dir))
                {
                    yield return item;
                }
            }
        }

        private (DriveInfo drive, DriveMeta driveMeta) ReadDriveMetaFromDisk(DirectoryInfo root)
        {
            var drive = new DriveInfo(root.Name);
            var driveMeta = _metaFactory.CreateDrive(drive);
            return (drive, driveMeta);
        }
    }
}