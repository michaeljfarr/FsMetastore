using System.Collections;
using System.Collections.Generic;

namespace FsMetastore.Persistence.Enumeration
{
    public class AppendEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _first;
        private readonly IEnumerable<T> _second;

        public AppendEnumerable(IEnumerable<T> first, IEnumerable<T> second)
        {
            _first = first;
            _second = second;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new AppendEnumerator<T>(_first.GetEnumerator(), _second.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}