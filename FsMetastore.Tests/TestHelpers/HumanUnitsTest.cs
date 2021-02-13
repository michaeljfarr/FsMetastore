using System;
using FluentAssertions;
using Xunit;

namespace FsMetastore.Tests.TestHelpers
{
    public class HumanUnitsTest
    {
        [Theory]
        [InlineData(5000, 1, "5.00 item/ms")]
        [InlineData(1000, 1, "1000.00 item/sec")]
        [InlineData(5, 1, "5.00 item/sec")]
        [InlineData(1, 1, "60.00 item/min")]
        [InlineData(1, 60, "60.00 item/hour")]
        [InlineData(2, 60, "2.00 item/min")]
        [InlineData(10, 60, "10.00 item/min")]
        public void TestGetHumanRate(int numItems, double numSeconds, string expected)
        {
            HumanUnits.GetHumanRate(numItems, TimeSpan.FromSeconds(numSeconds), "item").Should().Be(expected);
        }
    }
}