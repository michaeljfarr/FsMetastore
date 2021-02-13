using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FsMetastore.Persistence.IO.Test;

namespace FsMetastore.Persistence.IO.FsMetaDb
{
    class HierarchyOrderer
    {
        private readonly IOrderableHierarchy _hierarchy;
        private readonly ITestOutputer _testOutputer;
        private readonly IComparer<string> _stringComparer;
        private const uint MaxOrdOffset = 10000000;
        private readonly uint _ordOffset;

        public static uint OrdOffset(int currentGeneration)
        {
            if (currentGeneration <= 1)
            {
                return MaxOrdOffset;
            }

            var pow = Math.Pow(10, currentGeneration - 1);
            if (pow > uint.MaxValue)
            {
                return 1;
            }

            return MaxOrdOffset / Math.Min((uint)pow, MaxOrdOffset);
        }

        private readonly List<(int id, ulong expectedOrd)>
            _ordsThatNeedUpdating = new List<(int id, ulong expectedOrd)>();


        public HierarchyOrderer(IOrderableHierarchy hierarchy, ITestOutputer testOutputer, int currentGeneration,
            IComparer<string> stringComparer)
        {
            _hierarchy = hierarchy;
            _testOutputer = testOutputer;
            _stringComparer = stringComparer;
            _ordOffset = OrdOffset(currentGeneration);
        }

        public void UpdateOrds()
        {
            var sw = Stopwatch.StartNew();
            ulong maxOrd = 0;
            using (var transaction = _hierarchy.StartTransaction())
            {
                maxOrd = CheckOrd(null, _ordOffset, ulong.MaxValue);
                Flush(0);
                _hierarchy.CommitTransaction(transaction);
            }
            sw.Stop();
            _testOutputer.WriteLine($"Ords updated {maxOrd/MaxOrdOffset} files in {sw.Elapsed.TotalSeconds:N0}sec.");
        }

        private void Flush(int min)
        {
            if (_ordsThatNeedUpdating.Count >= min)
            {
                _hierarchy.UpdateOrds(_ordsThatNeedUpdating);
                _ordsThatNeedUpdating.Clear();
            }
        }

        private void PushOrds(List<(int id, ulong? currentOrd, ulong expectedOrd)> childOrds)
        {
            foreach (var childOrd in childOrds)
            {
                PushOrd(childOrd.id, childOrd.currentOrd, childOrd.expectedOrd);
            }
            Flush(5000);
        }

        private void PushOrd(int id, ulong? currentOrd, ulong expectedOrd)
        {
            if (currentOrd != expectedOrd)
            {
                _ordsThatNeedUpdating.Add((id, expectedOrd));
            }
        }
        
        

        private ulong CheckOrd(int? parentId, ulong @base, ulong lid)
        {
            var children = _hierarchy.GetChildren(parentId).OrderBy(a => a.name, _stringComparer).
                Select(a => (a.id, a.currentOrd)).ToList();

            var nextOrd = @base;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var childOrd = child.currentOrd == null ? nextOrd : Math.Max(child.currentOrd.Value, nextOrd);
                PushOrd(child.id, child.currentOrd, childOrd);
                nextOrd = CheckOrd(child.id, childOrd + _ordOffset, lid);
            }
            Flush(5000);

            //if the are n descendents nextOrd = @base + _ordOffset*(n+1)
            return nextOrd;
        }
    }
}