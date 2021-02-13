using System.Diagnostics;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IOC;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace FsMetastore.Tests
{
    [Collection("FsReaders")]
    [Trait("Category", "Unit")]
    public class TestCaptureSolutionDir
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestCaptureSolutionDir(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }


        [Theory]
        [InlineData(NameStorageStrategy.StringRef)]
        [InlineData(NameStorageStrategy.LocalString)]
        public async Task TestCaptureAndRead(NameStorageStrategy nameStorageStrategy)
        {
            var serviceProvider = TestHelpers.TestHelpers.Create();
            var batchScopeFactory = serviceProvider.GetRequiredService<IBatchScopeFactory>();
            
            var sourcePath = TestHelpers.TestHelpers.GetSolutionFolderToScan();
            var batchRoot = TestHelpers.TestHelpers.RecreateTestSubFolder($"{nameStorageStrategy}.Capt");

            //This reads from sourcePath into a meta-database at batchRoot
            var sw = Stopwatch.StartNew();
            await ImportFolder(nameStorageStrategy, batchScopeFactory, batchRoot, sourcePath);
            sw.Stop();


            if (nameStorageStrategy == NameStorageStrategy.StringRef)
            {
                using (var writeScope = batchScopeFactory.CreateWriteScope(batchRoot, StorageType.MetaStream, nameStorageStrategy, null))
                {
                    writeScope.OptimizeStringRefs();
                }
            }

            //this is just to work out how fast we can read the file information off disk
            var sw3 = Stopwatch.StartNew();
            await CrawlFilesystemButStoreNoData(nameStorageStrategy, batchRoot, sourcePath);
            sw3.Stop();


            //this read reads from our metadata database
            var sw2 = Stopwatch.StartNew();
            using (var writeScope = await batchScopeFactory.CreateReadScopeAsync(batchRoot))
            {
                foreach(var file in writeScope.EnumerateMetadata())
                {
                    //_testOutputHelper.WriteLine($"{file.Name.Value}");
                }

                foreach(var file in writeScope.EnumerateFolders())
                {
                    if (file != null)
                    {
                        //_testOutputHelper.WriteLine($"{file.Name.Value}");
                    }
                }

                sw2.Stop();
            }
            _testOutputHelper.WriteLine($"ReadFsTime={sw3.ElapsedMilliseconds}, CaptureFsTime={sw.ElapsedMilliseconds}, ReadFmbTime={sw2.ElapsedMilliseconds}");
        }

        internal static async Task ImportFolder(NameStorageStrategy nameStorageStrategy, IBatchScopeFactory batchScopeFactory,
            string batchRoot, string sourcePath)
        {
            // await batchScopeFactory.ImportFromFileSystem(batchRoot, sourcePath, StorageType.FileMetaDb);

            using (var writeScope = batchScopeFactory.CreateWriteScope(batchRoot, StorageType.MetaStream, nameStorageStrategy, sourcePath))
            {
                await writeScope.CaptureFileMetadata(sourcePath, StringRefInitType.Clean);
            }
        }

        private static async Task CrawlFilesystemButStoreNoData(NameStorageStrategy nameStorageStrategy, string batchRoot,
            string sourcePath)
        {
            var noPersisterServiceProvider = TestHelpers.TestHelpers.Create();
            var noPersisterBatchScopeFactory = noPersisterServiceProvider.GetRequiredService<IBatchScopeFactory>();

            await noPersisterBatchScopeFactory.ImportFromFileSystem(batchRoot, sourcePath, StorageType.FileMetaDb);
            // using (var writeScope = noPersisterBatchScopeFactory.CreateWriteScope(batchRoot, StorageType.NoopTimer, nameStorageStrategy, sourcePath))
            // {
            //     await writeScope.CaptureFileMetadata(sourcePath, StringRefInitType.Clean);
            // }
        }

        [Theory]
        [InlineData(NameStorageStrategy.StringRef)]
        [InlineData(NameStorageStrategy.LocalString)]
        public async Task TestRead(NameStorageStrategy nameStorageStrategy)
        {
            var (batchScopeFactory, batchRoot) = await BatchScopeFactory(nameStorageStrategy);

            //var sourcePath = @"C:\\Users\\Micha\\";
            var sw2 = Stopwatch.StartNew();
            int numFiles = 0;
            using (var writeScope = await batchScopeFactory.CreateReadScopeAsync(batchRoot))
            {
                foreach(var file in writeScope.EnumerateMetadata())
                {
                    _testOutputHelper.WriteLine($"{file.Name.Value}");
                    numFiles++;
                }
            }

            sw2.Stop();
            _testOutputHelper.WriteLine($"Files={numFiles}, Time={sw2.ElapsedMilliseconds}, Rate={numFiles/sw2.ElapsedMilliseconds}(f/ms)");
        }

        [Theory]
        [InlineData(NameStorageStrategy.StringRef)]
        [InlineData(NameStorageStrategy.LocalString)]
        public async Task TestMetaEnumerator(NameStorageStrategy nameStorageStrategy)
        {
            var (batchScopeFactory, batchRoot) = await BatchScopeFactory(nameStorageStrategy);

            //var sourcePath = @"C:\\Users\\Micha\\";
            var sw2 = Stopwatch.StartNew();
            int numFiles = 0;
            using (var readScope = await batchScopeFactory.CreateReadScopeAsync(batchRoot))
            {
                foreach (var info in readScope.ReadInfos())
                {
                    //Debug.WriteLine($"{info.FullPath}");
                    //_testOutputHelper.WriteLine($"{info.FullPath}");
                    numFiles++;
                }
            }

            sw2.Stop();
            _testOutputHelper.WriteLine($"Files={numFiles}, Time={sw2.ElapsedMilliseconds}, Rate={numFiles/sw2.ElapsedMilliseconds}(f/ms)");
        }

        private static async Task<(IBatchScopeFactory batchScopeFactory, string batchRoot)> BatchScopeFactory(NameStorageStrategy nameStorageStrategy)
        {
            var serviceProvider = TestHelpers.TestHelpers.Create();
            var batchScopeFactory = serviceProvider.GetRequiredService<IBatchScopeFactory>();
            var sourcePath = TestHelpers.TestHelpers.GetSolutionFolderToScan();
            var batchRoot = TestHelpers.TestHelpers.RecreateTestSubFolder($"{nameStorageStrategy}.Diffs");
            await ImportFolder(nameStorageStrategy, batchScopeFactory, batchRoot, sourcePath);
            return (batchScopeFactory, batchRoot);
        }
    }
}
