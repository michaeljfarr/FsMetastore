using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Tests.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace FsMetastore.Tests
{
    /*
Read FS 2.5M         | N/A           
Load 2.5M Files      | MetaStream       
                     | FileMetaDb     
Read MetaStream 2.5M | MetaStream       
Read Import 2.5M     | FileMetaDb     
Load 25 Files        | MetaStream       
                     | FileMetaDb     
2 Change in 2.5M     | CurrentStateDb
2 Change in 25       | CurrentStateDb
     */
    [Collection("FsReaders")]
    public class BenchmarkTests 
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public BenchmarkTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task RunBenchmarks()
        {
            var testMergeImport = new TestMergeImport(_testOutputHelper);
            //quick warmup
            await RunImporter(StorageType.MetaStream, StorageType.MetaStream, TestHelpers.TestHelpers.GetSolutionFolderToScan());
            await RunImporter(StorageType.FileMetaDb, StorageType.FileMetaDb, TestHelpers.TestHelpers.GetSolutionFolderToScan());
            
            // Read FS 2.41M files in 1.90min (21.19 files/ms)
            //Create NoopTimer 2.41M files in 1.74min (23.13 files/ms) with NoopTimer of 0B
            _testOutputHelper.WriteLine("\nCaseA ...");
            await RunImporter(StorageType.NoopTimer, StorageType.NoopTimer, "C:");
            
            // Create ScanDb 2.41M files in 96.22sec (25.05 files/ms) with ScanDb of 132.57MB
            // Run Diff 5 changed of 2.41M files in 2.07min (19.45 files/ms) with ScanDb of 1261B.
            _testOutputHelper.WriteLine("\nCaseB ...");
            await RunImporter(StorageType.MetaStream, StorageType.MetaStream, "C:");
            
            // Create ScanDb 2.41M files in 97.36sec (24.76 files/ms) with ScanDb of 132.57MB
            // Run Diff 4 changed of 2.41M files in 1.94min (20.76 files/ms) with ImportDb of 28.57KB.
            _testOutputHelper.WriteLine("\nCaseC ...");
            await RunImporter(StorageType.MetaStream, StorageType.FileMetaDb, "C:");
            
            // Create ScanDb 22.60k files in 680.7959ms (33.20 files/ms) with ImportDb of 3.39MB
            // Run Diff 192 changed of 22.60k files in 489.8622ms (46.14 files/ms) with ImportDb of 28.56KB.
            // Create ScanDb 2.41M files in 2.08min (19.29 files/ms) with ImportDb of 263.23MB
            // Run Diff 192 changed of 2.41M files in 2.58min (15.54 files/ms) with ImportDb of 44.57KB.            
            _testOutputHelper.WriteLine("\nCaseD ...");
            await RunImporter(StorageType.FileMetaDb, StorageType.FileMetaDb, "C:");
            
            // Create ScanDb 864 files in 115.0592ms (7.51 files/ms) with ScanDb of 60.01KB
            // Run Diff 5 changed of 868 files in 106.879ms (8.08 files/ms) with ScanDb of 937B.
            _testOutputHelper.WriteLine("\nCaseE ... ");
            await RunImporter(StorageType.MetaStream, StorageType.MetaStream, TestHelpers.TestHelpers.GetSolutionFolderToScan());
            
            // Create ScanDb 864 files in 92.135ms (9.38 files/ms) with ScanDb of 60.01KB
            // Run Diff 4 changed of 867 files in 107.9901ms (8.00 files/ms) with ImportDb of 28.56KB.
            _testOutputHelper.WriteLine("\nCaseF ...");
            await RunImporter(StorageType.MetaStream, StorageType.FileMetaDb, TestHelpers.TestHelpers.GetSolutionFolderToScan());
            
            async Task RunImporter(StorageType scanStorageType, StorageType diffStorageType, string sourcePath)
            {
                var stats = await testMergeImport.RunImportAndDiff(scanStorageType, diffStorageType, sourcePath);
                var scanSize = TestHelpers.TestHelpers.GetDirectorySize(testMergeImport.DbRoot);
                var diffSize = TestHelpers.TestHelpers.GetDirectorySize(testMergeImport.DiffRoot);
                //Read FS 2409845 files in 100697ms
                _testOutputHelper.WriteLine(
                    $"Create {scanStorageType} {HumanUnits.GetHumanAmount(stats.readStats.NumFilesFound)} files in {HumanUnits.GetHumanDuration(stats.captureTime.Elapsed)} ({HumanUnits.GetHumanRate(stats.readStats.NumFilesFound, stats.captureTime.Elapsed, "files")}) with {scanStorageType} of {HumanUnits.GetHumanSize(scanSize)}");
                if (stats.diffStats != null)
                {
                    _testOutputHelper.WriteLine(
                        $"Run Diff {HumanUnits.GetHumanAmount(stats.diffStats.NumFileChanges)} changed of {HumanUnits.GetHumanAmount(stats.diffStats.NumFilesFound)} files in {HumanUnits.GetHumanDuration(stats.diffTime.Elapsed)} ({HumanUnits.GetHumanRate(stats.readStats.NumFilesFound, stats.diffTime.Elapsed, "files")}) with {diffStorageType} of {HumanUnits.GetHumanSize(diffSize)}.");
                }
            }
        }
    }
}