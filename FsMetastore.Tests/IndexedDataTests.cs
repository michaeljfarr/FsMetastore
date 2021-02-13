using System;
using System.Collections.Generic;
using System.IO;
using FsMetastore.Model.Batch;
using FsMetastore.Model.StorageStrategy;
using FsMetastore.Persistence.IOC;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FsMetastore.Tests
{
    public class IndexedDataTests
    {
        private readonly Dictionary<uint, string> _testValues = new ()
        {
            {1, "one"},
            {8, "eight"},
            {90, ""},
            {91, "sentence with a few words and more than 8 characters"},
            {100, "100"},
            {101, "101"},
            {102, "102"},
            {Int16.MaxValue + 1, "2nd last"},
            {Int16.MaxValue + 2, "last"},
        };

        [Fact]
        [Trait("Category", "Unit")]
        public void TestWriteRead()
        {
            var serviceProvider = TestHelpers.TestHelpers.Create();
            var batchScopeFactory = serviceProvider.GetRequiredService<IBatchScopeFactory>();

            var outputDir = Path.GetFullPath("OutputDir");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            using (var writeScope = batchScopeFactory.CreateWriteScope(outputDir, StorageType.MetaStream, NameStorageStrategy.StringRef, null))
            {
                //write the _testValues to a file
                writeScope.WriteIndexedData(_testValues);

                //now make sure that each of the _testValues exist in the file.
                using (var indexedDataReader = writeScope.OpenIndexedDataReader())
                {
                    foreach (var pair in _testValues)
                    {
                        var itemValue = indexedDataReader.FindStringItem(pair.Key);
                        Assert.Equal(pair.Value, itemValue);
                    }
                }
            }
        }
    }
}