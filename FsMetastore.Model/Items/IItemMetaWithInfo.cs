namespace FsMetastore.Model.Items
{
    public interface IItemMetaWithInfo : IItemMeta
    {
        string FullPath { get; }
        string Name { get; }
        FolderMeta AsFolder { get; }
        FileMeta AsFile { get; }
        void AddPermission(PermissionMask permission);
    }

    public static class ItemMetaExtensions
    {
        public static bool HasPermission(this IItemMetaWithInfo itemMeta, PermissionMask permission)
        {
            return itemMeta.PermissionMask!=null && itemMeta.PermissionMask.Value.HasFlag(permission);
        }
    }
}