using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsMetastore.Model.Batch;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IO.Commands;
using FsMetastore.Persistence.IO.Test;
using FsMetastore.Persistence.IOC;
using FsMetastore.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace FsMetastore.Tests
{
    [Collection("FsReaders"), Trait("Category", "Unit")]
    public class TestMergeImport
    {
        public const string NewFileName = "diffFile.txt";
        public const string NewFolderName = "diffFoldr";
        private readonly ITestOutputHelper _testOutputHelper;
        public string DbRoot;
        public string DiffRoot;

        public TestMergeImport(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        //[InlineData(StorageType.MetaStream, StorageType.MetaStream)] //2kb
        [InlineData(StorageType.MetaStream, StorageType.FileMetaDb)] //28kb-32kb (32kb if auto VACUUM is enabled)
        [InlineData(StorageType.FileMetaDb, StorageType.FileMetaDb)] //28kb-32kb
        public async Task TestReadAndMerge(StorageType scanStorageType, StorageType diffStorageType)
        {
            var sourcePath = TestHelpers.TestHelpers.GetSolutionFolderToScan();
            await RunImportAndDiff(scanStorageType, diffStorageType, sourcePath);
        }

        public async Task<(Stopwatch captureTime, Stopwatch diffTime, BatchStatistics readStats, BatchStatistics diffStats)> RunImportAndDiff(StorageType scanStorageType, StorageType diffStorageType, string sourcePath, bool sameScanAndDiffBatch = false)
        {
            var nameStorageStrategy = NameStorageStrategy.LocalString;
            var serviceProvider = MakeServiceProvider();
            var batchScopeFactory = serviceProvider.GetRequiredService<IBatchScopeFactory>();
            var changesFolder = TestHelpers.TestHelpers.RecreateTestSubFolder($"{nameStorageStrategy}.Diffs");
            DbRoot = TestHelpers.TestHelpers.RecreateTestSubFolder($"{nameStorageStrategy}.merge1");
            DiffRoot = sameScanAndDiffBatch ?DbRoot : TestHelpers.TestHelpers.RecreateTestSubFolder($"{nameStorageStrategy}.merge2");

            var diffFile = Path.Combine(changesFolder, NewFileName);
            var diffFolder = Path.Combine(changesFolder, NewFolderName);

            //capture a folder and write it to one folder
            var captureTime = Stopwatch.StartNew();
            (BatchSource batchSource, BatchStatistics batchStatistics) readStats;
            using (var writeScope = batchScopeFactory.CreateWriteScope(DbRoot, scanStorageType, nameStorageStrategy, sourcePath))
            {
                readStats = await writeScope.CaptureFileMetadata(sourcePath, StringRefInitType.Clean);
            }
            // var readStats = await batchScopeFactory.ImportFromFileSystem(DbRoot, sourcePath);

            captureTime.Stop();

            if (scanStorageType == StorageType.NoopTimer)
            {
                return (captureTime, null, readStats.batchStatistics, null);
            }

            //change some data
            await File.WriteAllTextAsync(diffFile, "some stuff");
            Directory.CreateDirectory(diffFolder);

            var diffTime = Stopwatch.StartNew();
            //scan the folder again, but this time just write the diff, 
            if (DbRoot == DiffRoot)
            {
                // (BatchSource batchSource, BatchStatistics batchStatistics) diffStats;
                // using (var writeScope =
                //     batchScopeFactory.CreateWriteScope(DiffRoot, diffStorageType, nameStorageStrategy, sourcePath))
                // {
                //     diffStats = await writeScope.ImportDiff(DiffRoot, sourcePath);
                // }
                var diffStats = await batchScopeFactory.ImportFromFileSystem(DiffRoot, sourcePath, diffStorageType);

                diffTime.Stop();
                diffStats.batchSource.BatchInfo.Generation.Should().Be(2);

                await CheckOrds(batchScopeFactory, scanStorageType);
                
                var scanDbDiffRoot = TestHelpers.TestHelpers.RecreateTestSubFolder($"{nameStorageStrategy}.merge2");
                await batchScopeFactory.ExportMetaStream(DbRoot, scanDbDiffRoot, diffStats.batchSource.BatchInfo.Generation);
                
                return (captureTime, diffTime, readStats.batchStatistics, diffStats.batchStatistics);
            }
            else
            {

                // (BatchSource batchSource, BatchStatistics batchStatistics) diffStats;
                // using (var readScope = await batchScopeFactory.CreateReadScopeAsync(DbRoot))
                // // using (var writeScope =
                // //     batchScopeFactory.CreateWriteScope(DiffRoot, diffStorageType, nameStorageStrategy, sourcePath))
                // {
                //     diffStats = await batchScopeFactory.ImportFromFileSystem(DiffRoot, sourcePath);
                // }
                var diffStats = await batchScopeFactory.ImportFromFileSystem(DbRoot, sourcePath, diffStorageType);

                diffTime.Stop();

                await CheckOrds(batchScopeFactory, scanStorageType);

                return (captureTime, diffTime, readStats.batchStatistics, diffStats.batchStatistics);
            }
        }

        private IServiceProvider MakeServiceProvider()
        {
            var serviceProvider =
                TestHelpers.TestHelpers.Create(sc => sc.AddSingleton<ITestOutputer>(new XunitTestOutputer(_testOutputHelper)));
            return serviceProvider;
        }

        /// <summary>
        /// Confirms the invariant condition parent.ord &lt; child.ord within the database. 
        /// </summary>
        private async Task CheckOrds(IBatchScopeFactory batchScopeFactory, StorageType scanStorageType)
        {
            if (scanStorageType != StorageType.FileMetaDb)
            {
                return;
            }

            using (var readScope = await batchScopeFactory.CreateReadScopeAsync(DbRoot))
            {
                var importDbContext = readScope.GetImportDbMetaContext();
                var badOrds = importDbContext.SelectListOfInt32String($"select c.Id, c.Name from FolderMeta p inner join FolderMeta c on p.Id=c.ParentId where p.Ord>=c.Ord").ToList();
                badOrds.Should().BeEmpty();
                var nullOrds = importDbContext.SelectListOfInt32String($"select Id, Name from FolderMeta where Ord is null").ToList();
                nullOrds.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task TestImportDbAsDiff()
        {
            var sourcePath = TestHelpers.TestHelpers.GetSolutionFolderToScan();
            //sourcePath = "C:/Users/Micha";
            
            //Ords updated 28776 files in 4sec.
            //Create ImportDb 1.74M files in 89.45sec (19.43 files/ms) with ImportDb of 166.97MB
            //Run Diff 2 changed of 1.74M files in 82.90sec (20.97 files/ms) with ImportDb of 166.97MB.


            var scanStorageType = StorageType.FileMetaDb;
            var diffStorageType = scanStorageType;
            var stats = await RunImportAndDiff(scanStorageType, scanStorageType, sourcePath, true);
            var scanSize = TestHelpers.TestHelpers.GetDirectorySize(DbRoot);
            var diffSize = TestHelpers.TestHelpers.GetDirectorySize(DiffRoot);
            _testOutputHelper.WriteLine(
                $"Create {scanStorageType} {HumanUnits.GetHumanAmount(stats.readStats.NumFilesFound)} files in {HumanUnits.GetHumanDuration(stats.captureTime.Elapsed)} ({HumanUnits.GetHumanRate(stats.readStats.NumFilesFound, stats.captureTime.Elapsed, "files")}) with {scanStorageType} of {HumanUnits.GetHumanSize(scanSize)}");
            if (stats.diffStats != null)
            {
                _testOutputHelper.WriteLine(
                    $"Run Diff {HumanUnits.GetHumanAmount(stats.diffStats.NumFileChanges)} changed of {HumanUnits.GetHumanAmount(stats.diffStats.NumFilesFound)} files in {HumanUnits.GetHumanDuration(stats.diffTime.Elapsed)} ({HumanUnits.GetHumanRate(stats.readStats.NumFilesFound, stats.diffTime.Elapsed, "files")}) with {diffStorageType} of {HumanUnits.GetHumanSize(diffSize)}.");
            }
        }
        
        [Fact]
        public async Task TestExportThenApplyDiff()
        {
            var sourcePath = TestHelpers.TestHelpers.GetSolutionFolderToScan();
            //sourcePath = "C:/Users/Micha";
            
            var serviceProvider = MakeServiceProvider();
            var importCommandService = serviceProvider.GetRequiredService<IBatchCommandService>();
            
            var fileChangeCreator = new FileChangeCreator()
                .AddElement("testdir", true)
                .AddElement("testfile", false)
                .AddElement("testfile2", false)
                .AddElement("deepdir/deep/deep/testfile3", false);
            
            fileChangeCreator.Remove();

            var importDbFolder = TestHelpers.TestHelpers.RecreateTestSubFolder($"ImportDb.Db");
            var diffDbFolder = TestHelpers.TestHelpers.RecreateTestSubFolder($"ImportDb.Diff");
            var importDbCopyFolder = TestHelpers.TestHelpers.RecreateTestSubFolder($"ImportDb.Db.Copy");
            
            //create a db
            var createStats = await importCommandService.CreateImportDb(importDbFolder, sourcePath, BatchCommandType.WipeExisting);

            //save the original db to a second location
            fileChangeCreator.Copy(importDbFolder, importDbCopyFolder);
            
            //creates some new files to detect
            fileChangeCreator.CreateAll();
            
            //rescan the same location to pickup the new files
            var diffStats = await importCommandService.CreateImportDb(importDbFolder, sourcePath, BatchCommandType.ApplyDiff);
            
            //export the ScanDb diff 
            await importCommandService.ExportChangesAsMetaStream(importDbFolder, diffDbFolder, diffStats.batchSource.BatchInfo.Generation);
            
            //import the ScanDb diff into the copy
            await importCommandService.ImportChangesFromMetaStream(importDbCopyFolder, diffDbFolder);

        }
    }
}