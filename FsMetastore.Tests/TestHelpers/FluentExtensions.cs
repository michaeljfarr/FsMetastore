using FluentAssertions;
using FluentAssertions.Primitives;
using FsMetastore.Model.Items;

namespace FsMetastore.Tests.TestHelpers
{
    public static class FluentExtensions
    {
        public static AndConstraint<ObjectAssertions> BeNullOrHasFlag(this ObjectAssertions assertions, PermissionMask flag, string because = "", params object[] becauseArgs)
        {
            return assertions.Match((a=> a == null || ((PermissionMask?)a).Value.HasFlag(flag)), because, becauseArgs);
        }
        
        public static AndConstraint<ObjectAssertions> BeNullOrDoesntHaveFlag(this ObjectAssertions assertions, PermissionMask flag, string because = "", params object[] becauseArgs)
        {
            return assertions.Match((a=> a == null || !((PermissionMask?)a).Value.HasFlag(flag)), because, becauseArgs);
        }
    }
}