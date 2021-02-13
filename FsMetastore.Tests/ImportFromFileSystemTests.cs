using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IO.Test;
using FsMetastore.Persistence.IOC;
using FsMetastore.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace FsMetastore.Tests
{
    [Collection("FsReaders")]
    public class ImportFromFileSystemTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ImportFromFileSystemTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private static readonly Dictionary<string, IReadOnlyList<string>> FolderData = 
            new Dictionary<string, IReadOnlyList<string>>()
            {
              {"empty", new string[0]},
              {"onefolder", new []{"foldera"}},
              {"onefolderb", new []{"folderb"}},
              {"onefolderc", new []{"folderc"}},
              {"twofolders", new []{"foldera", "folderb"}},
              {"twofilesa", new []{"filea", "fileb"}},
              {"twofilesb", new []{"fileb", "filec"}},
              {"onefilea", new []{"filea"}},
              {"onefileb", new []{"fileb"}},
              {"onefilec", new []{"filec"}},
              {"deepfolder", new []{"folder/folder/folder/folder"}},
              {"filefolder", new []{"folder/folder/folder/file"}},
            };

        [Theory]
        [InlineData("empty","empty", "empty", 0, 0, 0)]
        [InlineData("onefolder","empty", "empty", 1, 0, 0)]
        [InlineData("twofolders","empty", "empty", 2, 0, 0)]
        [InlineData("deepfolder","empty", "empty", 4, 0, 0)]
        [InlineData("filefolder","empty", "empty", 4, 0, 0)]
        [InlineData("onefolder","onefolder", "empty", 1, 1, 0)]
        [InlineData("twofolders","twofolders", "empty", 2, 2, 0)]
        [InlineData("deepfolder","deepfolder", "empty", 4, 1, 0)]
        [InlineData("filefolder","filefolder", "empty", 4, 1, 0)]
        [InlineData("empty","empty", "onefolder", 1, 0, 1)]
        [InlineData("empty","empty", "twofolders", 2, 0, 2)]
        [InlineData("empty","empty", "deepfolder", 4, 0, 4)]
        [InlineData("onefolder","empty", "deepfolder", 5, 0, 4)]
        [InlineData("onefolder","empty", "filefolder", 5, 0, 4)]
        [InlineData("twofolders","onefolderb", "onefolderc", 3, 1, 1)]
        [InlineData("twofilesa","onefileb", "onefilec", 3, 1, 1)]
        [InlineData("twofilesb","onefileb", "onefilea", 3, 1, 1)]
        public async Task TestInlineData(string startWithKey, string deleteKey, string addKey, int expectedCount, int expectedDeleted, int expectedNew)
        {
            //its hard for the test code to work out what to expect when deleting, but the test cases are crafted 
            //so that a basic assumption works.

            var startWith = FolderData[startWithKey];
            var delete = FolderData[deleteKey];
            var add = FolderData[addKey];
            
            var serviceProvider = MakeServiceProvider();
            var batchScopeFactory = serviceProvider.GetRequiredService<IBatchScopeFactory>();
            var dbFolder = TestHelpers.TestHelpers.RecreateTestSubFolder($"ZipperDb");
            var sourceFolder = TestHelpers.TestHelpers.RecreateTestSubFolder($"ZipperTests");
            var folderHierarchy = sourceFolder.Count(a => a == '/' || a == '\\') + 1;

            foreach (var startItem in startWith)
            {
                await CreateItem(startItem, sourceFolder);
            }

            await batchScopeFactory.ImportFromFileSystem(dbFolder, sourceFolder, StorageType.FileMetaDb);
            // using (var writeScope = batchScopeFactory.CreateWriteScope(dbFolder, StorageType.FileMetaDb, NameStorageStrategy.LocalString, sourceFolder))
            // {
            //     await writeScope.CaptureFileMetadata(sourceFolder, StringRefInitType.Clean);
            // }
            
            foreach (var addItem in add)
            {
                await CreateItem(addItem, sourceFolder);
            }
            foreach (var deleteItem in delete)
            {
                DeleteItem(deleteItem, sourceFolder);
            }
            
            using (var batchScope = await batchScopeFactory.CreateReadScopeAsync(dbFolder, null, sourceFolder))
            {
                
                int totalNum = -folderHierarchy;
                int deletedNum = 0;
                int addedNum = 0;
                foreach (var a in await batchScope.ReadZip())
                {
                    totalNum++;
                    var changes = "[]";
                    if (a.PermissionMask == null || !a.PermissionMask.Value.HasFlag(PermissionMask.Unchanged))
                    {
                        if (a.PermissionMask?.HasFlag(PermissionMask.Deleted) == true)
                        {
                            changes = "[D]";
                            deletedNum++;
                        }
                        else
                        {
                            changes = "[A]";
                            addedNum++;
                        }
                    }
                    else
                    {
                        changes = "[]";
                    }
                    

                    _testOutputHelper.WriteLine($"{a.FullPath} {changes}");
                }

                totalNum.Should().Be(expectedCount);
                deletedNum.Should().Be(expectedDeleted);
                addedNum.Should().Be(expectedNew);
            }
        }
        
        
        [Theory]
        [InlineData("deepfolder","deepfolder", "empty", 4, 1, 0)]
        [InlineData("onefolder","empty", "filefolder", 5, 0, 4)]
        public async Task TestImportDiff(string startWithKey, string deleteKey, string addKey, int expectedCount, int expectedDeleted, int expectedNew)
        {
            //its hard for the test code to work out what to expect when deleting, but the test cases are crafted 
            //so that a basic assumption works.

            var startWith = FolderData[startWithKey];
            var delete = FolderData[deleteKey];
            var add = FolderData[addKey];
            
            var serviceProvider = MakeServiceProvider();
            var batchScopeFactory = serviceProvider.GetRequiredService<IBatchScopeFactory>();
            var dbFolder = TestHelpers.TestHelpers.RecreateTestSubFolder($"ZipperDb");
            var sourceFolder = TestHelpers.TestHelpers.RecreateTestSubFolder($"ZipperTests");
            var folderHierarchy = sourceFolder.Count(a => a == '/' || a == '\\') + 1;

            foreach (var startItem in startWith)
            {
                await CreateItem(startItem, sourceFolder);
            }

            // using (var writeScope = batchScopeFactory.CreateWriteScope(dbFolder, StorageType.FileMetaDb, NameStorageStrategy.LocalString, sourceFolder))
            // {
            //     await writeScope.CaptureFileMetadata(sourceFolder, StringRefInitType.Clean);
            // }
            
            await batchScopeFactory.ImportFromFileSystem(dbFolder, sourceFolder, StorageType.FileMetaDb);
            
            foreach (var addItem in add)
            {
                await CreateItem(addItem, sourceFolder);
            }
            foreach (var deleteItem in delete)
            {
                DeleteItem(deleteItem, sourceFolder);
            }

            await batchScopeFactory.ImportFromFileSystem(dbFolder, sourceFolder, StorageType.FileMetaDb);
            
            using (var batchScope = await batchScopeFactory.CreateReadScopeAsync(dbFolder, null, sourceFolder))
            {
                var importDbMetaContext = batchScope.GetImportDbMetaContext();
                var numFolders = importDbMetaContext.ExecuteScalarSqlInt32($"select count(*) from FolderMeta where ModifiedGeneration=2");
                var numFiles = importDbMetaContext.ExecuteScalarSqlInt32($"select count(*) from FileMeta where ModifiedGeneration=2");
                (numFolders + numFiles).Should().Be(expectedNew + expectedDeleted);
                
                var numDelFolders = importDbMetaContext.ExecuteScalarSqlInt32($"select count(*) from FolderMeta where ModifiedGeneration=2 AND (PermissionMask&{(int)PermissionMask.Deleted})!=0 ");
                var numDelFiles = importDbMetaContext.ExecuteScalarSqlInt32($"select count(*) from FileMeta where ModifiedGeneration=2 AND (PermissionMask&{(int)PermissionMask.Deleted})!=0 ");
                (numDelFolders + numDelFiles).Should().Be(expectedDeleted);
               
            }
        }

        private static async Task CreateItem(string startItem, string sourceFolder)
        {
            var leafName = Path.GetFileName(startItem);
            if (leafName.Contains("file", StringComparison.OrdinalIgnoreCase))
            {
                var subDirName = Path.GetDirectoryName(startItem);
                var folderName = string.IsNullOrEmpty(subDirName) ? sourceFolder : Path.Combine(sourceFolder, subDirName);
                Directory.CreateDirectory(folderName);
                await File.WriteAllTextAsync(Path.Combine(sourceFolder, startItem), "somestuff");
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(sourceFolder, startItem));
            }
        }
        
        private static void DeleteItem(string startItem, string sourceFolder)
        {
            var leafName = Path.GetFileName(startItem);
            if (leafName.Contains("file", StringComparison.OrdinalIgnoreCase))
            {
                var subDirName = Path.GetDirectoryName(startItem);
                var folderName = string.IsNullOrEmpty(subDirName) ? sourceFolder : Path.Combine(sourceFolder, subDirName);
                Directory.CreateDirectory(folderName);
                File.Delete(Path.Combine(sourceFolder, startItem));
            }
            else
            {
                Directory.Delete(Path.Combine(sourceFolder, startItem));
            }
        }

        private IServiceProvider MakeServiceProvider()
        {
            var serviceProvider = TestHelpers.TestHelpers.Create(sc => 
                sc.AddSingleton<ITestOutputer>(new XunitTestOutputer(_testOutputHelper)));
            return serviceProvider;
        }
        
        
        private static BatchSourceProvider GetBatchSourceProvider()
        {
            var batchSourceProvider = new BatchSourceProvider
                {BatchSource = new BatchSource() {Drive = new DriveMeta() {PathCaseRule = PathCaseRule.Ntfs}}};
            return batchSourceProvider;
        }

    }
}