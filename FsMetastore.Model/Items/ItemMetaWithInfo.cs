using System;

namespace FsMetastore.Model.Items
{
    public class ItemMetaWithInfo : IItemMetaWithInfo
    {
        private readonly IItemMeta _itemMeta;
        private readonly ItemMetaWithInfo _parent;

        public ItemMetaWithInfo(IItemMeta itemMeta, ItemMetaWithInfo parent)
        {
            _itemMeta = itemMeta;
            _parent = parent;
        }

        public int Id => _itemMeta.Id;

        public int? ParentId => _itemMeta.ParentId;

        public DateTimeOffset? ModifiedDate => _itemMeta.ModifiedDate;

        public PermissionMask? PermissionMask => _itemMeta.PermissionMask;

        public int? OwnerId => _itemMeta.OwnerId;

        public int? GroupId => _itemMeta.GroupId;

        public long? Position => _itemMeta.Position;

        public bool IsFolder => _itemMeta.IsFolder;

        private string _fullPath = null;
        public string FullPath 
        {
            get 
            { 
                if(_fullPath!=null)
                {
                    return _fullPath;
                }
                if (_parent == null)
                {
                    return Name;
                    //return !Name.EndsWith("/") ? $"{Name}/" : Name;
                }
                else
                {
                    var fullPath = $"{_parent.FullPath}/{Name}";
                    //var fullPath = _parent._parent == null ? $"{_parent.Name}/{Name}" : $"{_parent.FullPath}/{Name}";
                    if(Name == null)
                    {
                        //The Name is null because it is by ref, so lets not cache the full path until
                        //the Name is dereferenced.
                        return fullPath;
                    }
                    _fullPath = fullPath;
                    return _fullPath;
                }
            }
        }
        public string Name => IsFolder ? ((FolderMeta) _itemMeta).Name.Value : ((FileMeta) _itemMeta).Name.Value;

        public ItemMetaWithInfo Parent => _parent;

        public FolderMeta AsFolder => ( _itemMeta as FolderMeta);
        public FileMeta AsFile => ( _itemMeta as FileMeta);

        public void AddPermission(PermissionMask permission)
        {
            if (_itemMeta is FolderMeta folder)
            {
                folder.AddPermission(permission);
            }
            else if (_itemMeta is FileMeta file)
            {
                file.AddPermission(permission);
            }
        }

        public override string ToString()
        {
            return $"FullPath = {FullPath}";
        }

        public int NumChanges { get; set; }
    }
}