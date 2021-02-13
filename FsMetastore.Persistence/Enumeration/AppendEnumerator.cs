using System.Collections;
using System.Collections.Generic;

namespace FsMetastore.Persistence.Enumeration
{
    public class AppendEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _first;
        private readonly IEnumerator<T> _second;
        private bool _firstIsComplete;

        public AppendEnumerator(IEnumerator<T> first, IEnumerator<T> second)
        {
            _first = first;
            _second = second;
        }

        public bool MoveNext()
        {
            if (_firstIsComplete)
            {
                return _second.MoveNext();
            }
            else
            {
                if (!_first.MoveNext())
                {
                    _firstIsComplete = true;
                    return _second.MoveNext();
                }
                return true;
            }
        }

        public void Reset()
        {
            _firstIsComplete = false;
            _first.MoveNext();
            _second.MoveNext(); 
        }

        public T Current => _firstIsComplete ? _second.Current : _first.Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _second.Dispose();
            _first.Dispose();
        }
    }
}