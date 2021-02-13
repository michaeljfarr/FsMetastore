using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using FsMetastore.Model.Items;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IO.Change;
using FsMetastore.Persistence.IOC;
using FsMetastore.Persistence.Meta;
using FsMetastore.Persistence.Zipper;
using FsMetastore.Persistence.Zipper.Model;
using FsMetastore.Tests.TestHelpers;
using Xunit;

namespace FsMetastore.Tests
{
    [Trait("Category", "Unit")]
    public class FileMetaStreamZipperTests
    {
        private static readonly IComparer<string> StringComparer = System.StringComparer.Ordinal;
        
        [Theory]
        //check that A is treated as before a (bc ascii A = 65, a = 97)
        [InlineData("C:/A/bin", "C:/aaa", ComparisonResult.FirstBeforeSecond)]
        //checks that '/' (47) is ordered first (as ascii 1)
        [InlineData("C:/A.B", "C:/A/B", ComparisonResult.SecondBeforeFirst)]
        //checks that '/' (47) is ordered first (as ascii 1)
        [InlineData("C:/A B", "C:/A/B", ComparisonResult.SecondBeforeFirst)]
        public void TestCompareFilePaths(string path1, string path2, ComparisonResult comparisonResult)
        {
            PathComparator.CompareFilePaths(path1, path2, StringComparer).Should().Be(comparisonResult);
        }
        
        [Fact]
        public void TestChangeDetector_RootOnly()
        {
            var baseDrive = "C:"; 
            var baseDir = CreateItemFolderMeta(null, baseDrive, 1);
            var parent = new ItemMetaWithInfo(baseDir, null);
            var infos = new IItemMetaWithInfo[] {parent};
            //var changeDetector = new MetaChangeDetector(metaEnumerator);
            var metaSource = new IZipperItem[]
            {
                CreateDriveZipperItem(baseDrive),
                CreateDirectoryZipperItem($"{baseDrive}/"),
            };
            var changeDetector = new FileMetaStreamZipper(new MetaFactory(), PathStringComparerProvider.MatchCase(), metaSource, infos );
            var changes = changeDetector.Process().ToList();
            changes.Should().HaveCount(1);
            changes.Single().Name.Should().Be(baseDir.Name.Value);
            changes.Single().PermissionMask.Should().HaveFlag(PermissionMask.Unchanged);
        }
        
        [Fact]
        public void TestChangeDetector_ExistingItem()
        {
            int id = 1;
            var baseDrive = "C:"; 
            var baseDir = CreateItemFolderMeta(null, baseDrive, id++);
            var parent = new ItemMetaWithInfo(baseDir, null);

            var itemName = "ItemName";
            var item = CreateItemFolderMeta(baseDir, itemName, id++);
            var existingItem = new ItemMetaWithInfo(item, parent);
            
            var infos = new IItemMetaWithInfo[] {parent, existingItem};
            var metaSource = new IZipperItem[]
            {
                CreateDriveZipperItem(baseDrive),
                CreateDirectoryZipperItem($"{baseDrive}/"),
                CreateDirectoryZipperItem($"{baseDrive}/{itemName}")
            };            
            var changeDetector = new FileMetaStreamZipper(new MetaFactory(), PathStringComparerProvider.MatchCase(), metaSource, infos );
            
            var changes = changeDetector.Process().ToList();
            changes.Should().HaveCount(2);
            changes[0].Name.Should().MatchPathCaseSensitive(metaSource[0]);
            changes[0].PermissionMask.Should().HaveFlag(PermissionMask.Unchanged);

            changes[1].FullPath.Should().MatchPathCaseSensitive(metaSource[2]);
            changes[1].PermissionMask.Should().HaveFlag(PermissionMask.Unchanged);
        }

        private static FileZipperItem CreateFileZipperItem(string path)
        {
            return new FileZipperItem(new FileInfo(path));
        }
        
        private static DirectoryZipperItem CreateDirectoryZipperItem(string path)
        {
            return new DirectoryZipperItem(new DirectoryInfo(path));
        }

        private static DriveZipperItem CreateDriveZipperItem(string baseDrive)
        {
            return new DriveZipperItem(new DriveInfo(baseDrive), new MetaFactory().CreateDrive(new DriveInfo(baseDrive)));
        }

        [Fact]
        public void TestChangeDetector_NewFolder()
        {
            int id = 1;
            var baseDrive = "C:"; 
            var baseDir = CreateItemFolderMeta(null, baseDrive, id++);
            var parent = new ItemMetaWithInfo(baseDir, null);
            
            var itemName = "ItemName";

            var infos = new IItemMetaWithInfo[] {parent};
            
            var metaSource = new IZipperItem[]
            {
                CreateDriveZipperItem(baseDrive),
                CreateDirectoryZipperItem($"{baseDrive}/"),
                //this folder is in the source, but not in the baseline
                CreateDirectoryZipperItem($"{baseDrive}/{itemName}")
            };            
            var changeDetector = new FileMetaStreamZipper(new MetaFactory(), PathStringComparerProvider.MatchCase(), metaSource, infos );
            var changes = changeDetector.Process().ToList();

            changes.Should().HaveCount(2);
            changes[0].Name.Should().MatchPathCaseSensitive(metaSource[0]);
            changes[0].PermissionMask.Should().HaveFlag(PermissionMask.Unchanged);

            changes[1].FullPath.Should().MatchPathCaseSensitive(metaSource[2]);
            //this folder was added, so shouldn't have the unchanged flag
            changes[1].PermissionMask.Should().NotHaveFlag(PermissionMask.Unchanged);
        }
        
        
        [Theory]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void TestChangeDetector_FolderChangesAtLevel1(bool withFile, bool deleteFolder)
        {
            int id = 1;
            var baseDrive = "C:"; 
            var baseDir = CreateItemFolderMeta(null, baseDrive, id++);
            var parent = new ItemMetaWithInfo(baseDir, null);
            
            var folderName = "FolderName";
            var folderMeta = CreateItemFolderMeta(baseDir, folderName, id++);
            var folderItem = new ItemMetaWithInfo(folderMeta, parent);
            
            var file2Name = "File2";
            var file2Meta = CreateFileMeta(folderMeta, file2Name, id++);
            var file2Item = new ItemMetaWithInfo(file2Meta, folderItem);
            
            var baselineInfo = withFile ? new IItemMetaWithInfo[] {parent, folderItem, file2Item} : new IItemMetaWithInfo[] {parent, folderItem};

            var metaSource = new List<IZipperItem>()
            {
                CreateDriveZipperItem(baseDrive),
                CreateDirectoryZipperItem($"{baseDrive}/"),
            };

            var directoryZipperItem = CreateDirectoryZipperItem($"{baseDrive}/{folderName}");
            var fileZipperItem = CreateFileZipperItem($"{baseDrive}/{folderName}/{file2Name}");
            if (!deleteFolder)
            {
                metaSource.Add(directoryZipperItem);
                if (withFile)
                {
                    metaSource.Add(fileZipperItem);
                }
            }
            
            var changeDetector = new FileMetaStreamZipper(new MetaFactory(), PathStringComparerProvider.MatchCase(), metaSource, baselineInfo );
            var changes = changeDetector.Process().ToList();
            if (withFile)
            {
                changes.Should().HaveCount(3);
            }
            else
            {
                changes.Should().HaveCount(2);
            }

            changes[0].Name.Should().MatchPathCaseSensitive(metaSource[0]);
            changes[0].PermissionMask.Should().HaveFlag(PermissionMask.Unchanged);

            changes[1].FullPath.Should().MatchPathCaseSensitive(directoryZipperItem);
            if (deleteFolder)
            {
                changes[1].PermissionMask.Should().HaveFlag(PermissionMask.Deleted);
            }
            else
            {
                changes[1].PermissionMask.Should().HaveFlag(PermissionMask.Unchanged);
            }

            if (withFile)
            {
                changes[2].FullPath.Should().MatchPathCaseSensitive(fileZipperItem);
                if (deleteFolder)
                {
                    changes[2].PermissionMask.Should().HaveFlag(PermissionMask.Deleted);
                }
                else
                {
                    changes[2].PermissionMask.Should().HaveFlag(PermissionMask.Unchanged);
                }
            }
        }
        
        [Fact]
        public void TestChangeDetector_OldFolderFilesNotLostBecauseOfNewFolder()
        {
            int id = 1;
            var baseDrive = "C:"; 
            var baseDir = CreateItemFolderMeta(null, baseDrive, id++);
            var parent = new ItemMetaWithInfo(baseDir, null);

            var newFolderName = "ANewFolder";
            
            var folderName = "FolderName";
            var folderMeta = CreateItemFolderMeta(baseDir, folderName, id++);
            var folderItem = new ItemMetaWithInfo(folderMeta, parent);
            
            var file2Name = "File2";
            var file2Meta = CreateFileMeta(folderMeta, file2Name, id++);
            var file2Item = new ItemMetaWithInfo(file2Meta, parent);
            
            var baselineInfo = new IItemMetaWithInfo[] {parent, folderItem, file2Item};
            
            var metaSource = new List<IZipperItem>()
            {
                CreateDriveZipperItem(baseDrive),
                CreateDirectoryZipperItem($"{baseDrive}/"),
                CreateFileZipperItem($"{baseDrive}/{file2Name}"),
                //this folder is not in the baseline
                CreateDirectoryZipperItem($"{baseDrive}/{newFolderName}"),
                CreateDirectoryZipperItem($"{baseDrive}/{folderName}"),
            };

            var changeDetector = new FileMetaStreamZipper(new MetaFactory(), PathStringComparerProvider.MatchCase(), metaSource, baselineInfo );
            var changes = changeDetector.Process().ToList();
            changes.Should().HaveCount(5);
            changes[0].Name.Should().MatchPathCaseSensitive(metaSource[0]);
            changes[0].PermissionMask.Should().NotHaveFlag(PermissionMask.Deleted);

            changes[1].FullPath.Should().MatchPathCaseSensitive(metaSource[2]);
            changes[1].PermissionMask.Should().BeNull();

            //this is the folder that was added.
            changes[2].FullPath.Should().MatchPathCaseSensitive(metaSource[3]);
            changes[2].PermissionMask.Should().NotHaveFlag(PermissionMask.Unchanged);

            changes[3].FullPath.Should().MatchPathCaseSensitive(metaSource[4]);
            changes[3].PermissionMask.Should().HaveFlag(PermissionMask.Unchanged);
            

        }



        private static FolderMeta CreateItemFolderMeta(FolderMeta parent, string itemName, int id)
        {
            return new FolderMeta()
            {
                Id = id,
                ParentId = parent?.Id,
                Name = new StoredString()
                {
                    StorageType = StringStorageType.Undef,
                    Value = itemName.TrimEnd('/', '\\')
                }
            };
        }
        
        private static FileMeta CreateFileMeta(FolderMeta parent, string itemName, int id)
        {
            return new FileMeta()
            {
                FolderId = parent.Id,
                Name = new StoredString()
                {
                    StorageType = StringStorageType.Undef,
                    Value = itemName
                },
                PermissionMask = null,
                Id = id,
                FileLength = 5
            };
        }

    }
}
