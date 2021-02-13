using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FsMetastore.Persistence.IO.FileBatches
{
    public class PathStack
    {
        private readonly List<(int id, int?parentId, string name)> _pathItems;
        private readonly StringBuilder _sb = new StringBuilder(1024);

        public PathStack()
        {
            _pathItems = new List<(int id, int? parentId, string name)>();
        }

        public int? CurrentFolderId => _pathItems.LastOrDefault().id;

        public void Clear()
        {
            _pathItems.Clear();
        }
        public void NextItem(int id, int? parentId, string name)
        {
            if (parentId == null)
            {
                _pathItems.Clear();
                _pathItems.Add((id, null, name));
                return;
            }

            while (_pathItems.Any())
            {
                var lastItem = _pathItems.Last();
                if (lastItem.id != parentId.Value)
                {
                    _pathItems.RemoveAt(_pathItems.Count - 1);
                }
                else
                {
                    break;
                }
            }

            if (!_pathItems.Any())
            {
                throw new ApplicationException($"Failed to find parent with id {parentId}");
            }
            _pathItems.Add((id, parentId, name));
        }

        public string GetPath(string leafName)
        {
            if (_pathItems.Count == 0)
            {
                return leafName;
            }
            // bool folderIdFound = _pathItems.Any(a=>a.id == folderId);
            // if (!folderIdFound)
            // {
            //     return null;
            // }
            _sb.Clear();

            foreach (var pathItem in _pathItems)
            {
                _sb.Append(pathItem.name);
                _sb.Append('/');
                // if (pathItem.id == folderId)
                // {
                //     break;
                // }
            }

            if (leafName != null)
            {
                _sb.Append(leafName);
            }
            else
            {
                _sb.Length--;
            }

            return _sb.ToString();
        }
    }
}