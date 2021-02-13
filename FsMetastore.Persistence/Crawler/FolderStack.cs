using System;
using System.Collections.Generic;
using System.IO;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.IO.Change;

namespace FsMetastore.Persistence.Crawler
{
    class FolderStack
    {
        private ItemMetaWithInfo _topOfStack = null;

        public FolderStack()
        {
        }
        
        public FolderStack(DriveMeta driveMeta)
        {
            _topOfStack = new ItemMetaWithInfo(driveMeta.RootFolder, null);
        }

        /// <summary>
        /// This will track the current folder stack, removing previous in the items
        /// that are no longer part of the stack and then adding to the current stack
        /// </summary>
        /// <remarks>
        /// Call this each time you find a new folder when reading the FolderStream. 
        /// </remarks>
        public ItemMetaWithInfo Follow(FolderMeta folder)
        {
            FollowChanges(folder);
            return Current;
        }

        public List<ItemMetaWithInfo> FollowChanges(FolderMeta folder)
        {
            var oldTop = Descend(folder);
            if(_topOfStack == null)
            {
                //_source
                _topOfStack = new ItemMetaWithInfo(folder, null);
            }
            else
            {
                _topOfStack = new ItemMetaWithInfo(folder, _topOfStack);
            }

            return oldTop;
        }

        public List<ItemMetaWithInfo> Descend(IItemMeta folder)
        {
            var oldTopOfStack = new List<ItemMetaWithInfo>(); 
            while (_topOfStack != null && _topOfStack.Id != folder.ParentId)
            {
                oldTopOfStack.Add(_topOfStack);
                _topOfStack = _topOfStack.Parent;
            }

            return oldTopOfStack;
        }
        
        public void Descend(DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Parent == null)
            {
                while (_topOfStack.Parent != null)
                {
                    _topOfStack = _topOfStack.Parent;
                }
            }
            else
            {
                while (_topOfStack != null && PathComparator.CompareFilePaths(directoryInfo.Parent.FullName, _topOfStack.FullPath,
                    StringComparer.Ordinal)!=ComparisonResult.Same)
                {
                    _topOfStack = _topOfStack.Parent;
                }
            }

            if (_topOfStack == null)
            {
                throw new ApplicationException("Failed to Descend.");
            }
        }

        public ItemMetaWithInfo Current => _topOfStack;
    }
}
