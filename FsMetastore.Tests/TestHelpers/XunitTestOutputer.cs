using FsMetastore.Persistence.IO.Test;
using Xunit.Abstractions;

namespace FsMetastore.Tests.TestHelpers
{
    public class XunitTestOutputer : ITestOutputer
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XunitTestOutputer(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void WriteLine(string message)
        {
            _testOutputHelper.WriteLine(message);
        }
    }
}