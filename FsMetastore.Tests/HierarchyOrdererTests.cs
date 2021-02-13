using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FsMetastore.Persistence.IO.FsMetaDb;
using FsMetastore.Persistence.IO.Test;
using NSubstitute;
using Xunit;

namespace FsMetastore.Tests
{
    [Trait("Category", "Unit")]
    public class HierarchyOrdererTests
    {
        [Fact]
        public void TestSimpleHierarchy()
        {
            var hierarchyData = new List<(int? parentId, int id, string name, ulong? expectedOrdRelative, string path)>() 
            {
                (null, 1, "root", 1, "/root"),
                (1, 2, "b", 3, "/root/b"),
                (1, 5, "c", 7, "/root/c"),
                (1, 6, "a", 2, "/root/a"),
                (2, 3, "c", 5, "/root/b/c"),
                (2, 4, "b", 4, "/root/b/b"),
                (3, 7, "a", 6, "/root/b/c/a"),
            };
            var outputItems = new List<(int id, ulong expectedOrd)>();
            var hierarchy = Substitute.For<IOrderableHierarchy>();
            hierarchy.GetChildren(Arg.Any<int?>()).Returns(a => hierarchyData.Where(b => b.parentId == a.ArgAt<int?>(0)).Select(b=>(b.id, b.name, currentOrd:(ulong?)null)));
            hierarchy.When(a=>a.UpdateOrds(Arg.Any<List<(int id, ulong expectedOrd)>>())).Do(x => {
                outputItems.AddRange(x.ArgAt<List<(int id, ulong expectedOrd)>>(0));
            });
            var orderer = new HierarchyOrderer(hierarchy, new NullTestOutputer(), 1, StringComparer.Ordinal);
            orderer.UpdateOrds();

            outputItems.Should().HaveCount(hierarchyData.Count);
            hierarchyData = hierarchyData.OrderBy(a => a.expectedOrdRelative).ToList();
            outputItems = outputItems.OrderBy(a => a.expectedOrd).ToList();
            for(int i = 0; i<hierarchyData.Count; i++)
            {
                outputItems[i].id.Should().Be(hierarchyData[i].id, $"{hierarchyData[i].path} should be at position {i}");
            }
        }

        [Theory]
        [InlineData(1, 10000000)]
        [InlineData(2, 1000000)]
        [InlineData(8, 1)]
        [InlineData(100, 1)]
        [InlineData(100000000, 1)]
        public void TestOrdOffset(int generation, uint expectedOffset)
        {
            HierarchyOrderer.OrdOffset(generation).Should().Be(expectedOffset);
        }
        
        [Fact]
        public void TestLargeOrdOffsets()
        {
            for (int i = 8; i < 105; i++)
            {
                HierarchyOrderer.OrdOffset(i).Should().Be(1);
            }
        }
    }
}