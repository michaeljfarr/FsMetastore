using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsMetastore.Model.Batch;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IO.Test;
using FsMetastore.Persistence.IOC;
using FsMetastore.Persistence.PathHash;
using FsMetastore.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace FsMetastore.Tests
{
    [Collection("FsReaders")]
    public class PathHashTests 
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public PathHashTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task TestCreateIndex()
        {
            var nameStorageStrategy = NameStorageStrategy.LocalString;
            var serviceProvider = TestHelpers.TestHelpers.Create(sc => 
                sc.AddSingleton<ITestOutputer>(new XunitTestOutputer(_testOutputHelper)));
            
            var batchScopeFactory = serviceProvider.GetRequiredService<IBatchScopeFactory>();
            var batchRoot = TestHelpers.TestHelpers.RecreateTestSubFolder(nameStorageStrategy.ToString());
            var sourcePath = TestHelpers.TestHelpers.GetSolutionFolderToScan();
            if(!Directory.Exists(batchRoot))
            {
                Directory.CreateDirectory(batchRoot);
            }

            await TestCaptureSolutionDir.ImportFolder(nameStorageStrategy, batchScopeFactory, batchRoot, sourcePath);

            var hashCalculator = serviceProvider.GetRequiredService<IPathHashCalculator>();

            using (var writeScope = await batchScopeFactory.CreateReadScopeAsync(batchRoot))
            {
                writeScope.WritePathIndex();
            }

            using (var readScope = await batchScopeFactory.CreateReadScopeAsync(batchRoot))
            {
                var indexReader = new SqlitePathIndexReader(new BatchIOConfig() {BatchPathRoot = batchRoot});
                indexReader.InitDb();
                var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                var metaReader = readScope.OpenMetaReader();
                var sw = Stopwatch.StartNew();
                foreach (var file in files)
                {
                    if (Path.GetFileName(file) == "pathindex.db")
                    {
                        //this wont have a hash because we just created it.
                        return;
                    }
                    var exampleHash = hashCalculator.CalculatePathHash(file, true);
                    var hashPositions = indexReader.ReadPotentialMetaPositions(exampleHash).Distinct().ToList();
                    //this is flaky because of other tests that write to the same directory.
                    hashPositions.Any().Should().BeTrue($"{file} should have hash position");
                    var position = hashPositions.Single();
                    var fileMeta = metaReader.ReadFileAt(position);
                    fileMeta.Should().NotBeNull();
                    fileMeta.Should().NotBeNull();
                    fileMeta?.Name?.Value.Should().Be(Path.GetFileName(file));
                }
                sw.Stop();
                //eg: 1732 read in 1015ms or 1704.92/s
                //    1732 read in 765ms or 2261.68/s
                _testOutputHelper.WriteLine($"{files.Length} read in {sw.ElapsedMilliseconds}ms or {files.Length/sw.Elapsed.TotalSeconds:0.00}/s");
                metaReader.Close();
            }
        }
    }
}
