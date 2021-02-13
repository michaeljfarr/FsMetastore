using System;
using System.Collections.Generic;
using FsMetastore.Model.Items;
using FsMetastore.Persistence.Crawler;
using FsMetastore.Persistence.IO.Change;
using FsMetastore.Persistence.IOC;
using FsMetastore.Persistence.Meta;
using FsMetastore.Persistence.Zipper.Model;

namespace FsMetastore.Persistence.Zipper
{
    /// <summary>
    /// Merges the _fileMetaSource enumerable with _fileMetaBaseline to a new IEnumerable
    /// </summary>
    public class FileMetaStreamZipper 
    {
        private readonly IMetaFactory _metaFactory;
        private readonly IPathStringComparerProvider _batchSourceProvider;
        private readonly IEnumerator<IZipperItem> _fileMetaSource;
        private bool _sourceComplete;
        private readonly IEnumerator<IItemMetaWithInfo> _fileMetaBaseline;
        private bool _baselineComplete;
        private NextItemFrom _sourceToBaselineCompare;
        private readonly FolderStack _folderStack;

        public FileMetaStreamZipper(IMetaFactory metaFactory, IPathStringComparerProvider batchSourceProvider, IEnumerable<IZipperItem> fileMetaSource, IEnumerable<IItemMetaWithInfo> fileMetaBaseline)
        {
            _metaFactory = metaFactory;
            _batchSourceProvider = batchSourceProvider;
            _fileMetaSource = fileMetaSource.GetEnumerator();
            if (!_fileMetaSource.MoveNext())
            {
                throw new ApplicationException("fileMetaSource empty.");
            }

            var driveZipperItem = (DriveZipperItem) _fileMetaSource.Current;
            if (driveZipperItem == null)
            {
                throw new ApplicationException("fileMetaSource had null DriveMeta.");
            }

            _fileMetaBaseline = fileMetaBaseline.GetEnumerator();
            _folderStack = new FolderStack(driveZipperItem.DriveMeta);
            _sourceComplete = !_fileMetaSource.MoveNext();
            MoveNext(false, true, true);
        }

        private NextItemFrom SourceToBaselineCompare()
        {
            if (_baselineComplete && _sourceComplete)
            {
                return NextItemFrom.None;
            }
            if (_baselineComplete || _fileMetaBaseline.Current == null)
            {
                return NextItemFrom.Source;
            }
            if (_sourceComplete || _fileMetaSource.Current == null)
            {
                return NextItemFrom.Baseline;
            }

            var comparisonResult = PathComparator.CompareFilePaths(((IZipperPathItem)_fileMetaSource.Current).FullPath, _fileMetaBaseline.Current.FullPath, _batchSourceProvider.StringComparer);

            switch (comparisonResult)
            {
                case ComparisonResult.FirstBeforeSecond:
                    return NextItemFrom.Source;
                case ComparisonResult.Same:
                    return NextItemFrom.Both;
                case ComparisonResult.SecondBeforeFirst:
                    return NextItemFrom.Baseline;
                default:
                    throw new ArgumentOutOfRangeException(nameof(comparisonResult), comparisonResult, "Unknown comparison result.");
            }
        }

        private bool NextSource()
        {
            var next = _fileMetaSource.MoveNext();
            //the source can push null entries in to tell us that it has finished reading files
            //we dont need that hint, so just ignore it.
            while (next && _fileMetaSource.Current == null)
            {
                next = _fileMetaSource.MoveNext();
            }

            if (!next)
            {
                _sourceComplete = true;
            }

            return next;
        }

        private bool MoveBothNext()
        {
            return MoveNext(true, true);
        }
        
        private bool MoveNext(bool source, bool baseline, bool isFirst = false)
        {
            var foundNextSource = source && NextSource();
            var foundNextBaseline = baseline && _fileMetaBaseline.MoveNext();
            if(baseline && !foundNextBaseline) _baselineComplete = true;

            if (isFirst && _baselineComplete)
            {
                //this will happen whenever we are reading an empty baseline
                //we need to skip updating the Stack, because the correct item was created during initialization.
                return foundNextSource;
            }

            _sourceToBaselineCompare = SourceToBaselineCompare();
            
            UpdateStack();

            return foundNextSource || foundNextBaseline;
        }

        /// <summary>
        /// A new sourceItem has been discovered, make sure the stack represents the ancestors of the current source
        /// item, but prefer the baseline if it is available because it will have the correct id.
        /// </summary>
        private void UpdateStack()
        {
            //if the folders match, use the folder from baseline because it will already have the correct id.
            if (_sourceToBaselineCompare == NextItemFrom.Both || _sourceToBaselineCompare == NextItemFrom.Baseline)
            {
                if (_fileMetaBaseline.Current?.IsFolder != true)
                {
                    return;
                }
                _folderStack.Follow(_fileMetaBaseline.Current.AsFolder);
                return;
            }
            
            if (_sourceToBaselineCompare == NextItemFrom.Source && _fileMetaSource.Current is DirectoryZipperItem newSourceDir)
            {
                //the item is a new folder, so we need to create a folder.
                //it is either a direct child of _topOfStack, or a child of one of the items in _topOfStack,
                //so look down the stack and find the one where the full paths match
                _folderStack.Descend(newSourceDir.DirectoryInfo);
                //now _topOfStack must be the parent of dir, so create a new dir from it
                var sourceFolder = _metaFactory.CreateFolder(newSourceDir.DirectoryInfo, _folderStack.Current.AsFolder, null);
                //now just add it to the stack.
                _folderStack.Follow(sourceFolder);
                return;
            }
        }


        private bool MoveSourceNext()
        {
            return MoveNext(true, false);
        }

        private bool MoveBaselineNext()
        {
            return MoveNext(false, true);
        }
        
        public IEnumerable<IItemMetaWithInfo> Process()
        {
            while (!_baselineComplete || !_sourceComplete)
            {
                //equal items
                while (!_baselineComplete && !_sourceComplete && _sourceToBaselineCompare == NextItemFrom.Both && _fileMetaBaseline.Current != null)
                {
                    _fileMetaBaseline.Current.AddPermission(PermissionMask.Unchanged);

                    yield return _fileMetaBaseline.Current;
                    MoveBothNext();
                }
                //deleted items
                while (!_baselineComplete && _sourceToBaselineCompare == NextItemFrom.Baseline && _fileMetaBaseline.Current != null)
                {
                    //this isn't particularly efficient because the caller needs to process all of the deleted 
                    //items from the baseline.  however, the caller can just enumerate over file and subfolders.
                    //perhaps we can implement a skip function for that case ... but will have to move away from
                    //IEnumerable approach for the baseline.
                    _fileMetaBaseline.Current.AddPermission(PermissionMask.Deleted);

                    yield return _fileMetaBaseline.Current;
                    MoveBaselineNext();
                }
                //added items
                while (!_sourceComplete && (_baselineComplete || _sourceToBaselineCompare == NextItemFrom.Source))
                {
                    if (_fileMetaSource.Current is FileZipperItem file)
                    {
                        var newItem = _metaFactory.CreateFile(file.FileInfo, _folderStack.Current.AsFolder, null);
                        
                        yield return new ItemMetaWithInfo(newItem, _folderStack.Current);
                    }
                    else if (_fileMetaSource.Current is DirectoryZipperItem)
                    {
                        yield return _folderStack.Current;
                    }
                    else
                    {
                        throw new ApplicationException($"Unknown _fileMetaSource.Current type {_fileMetaSource.Current}.");
                    }

                    MoveSourceNext();
                }
            }
        }
    }
}