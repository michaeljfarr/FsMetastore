using System;
using System.Collections.Generic;

namespace FsMetastore.Persistence.Enumeration
{
    public class PutBackEnumerator<T> : IPutBackEnumerator<T> where T: class
    {
        private readonly IEnumerator<T> _enumerator;
        private bool _hasPutBackItem;
        private bool _wasAtEnd;

        public PutBackEnumerator(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            if (_hasPutBackItem)
            {
                _hasPutBackItem = false;
                return !_wasAtEnd;
            }
            else
            {
                var res = _enumerator.MoveNext();
                if (!res)
                {
                    _wasAtEnd = true;
                }

                return res;
            }
        }

        public void Reset()
        {
            _hasPutBackItem = false;
            _wasAtEnd = false;
            _enumerator.Reset();
        }

        public void PutBack()
        {
            if (_hasPutBackItem)
            {
                throw new ApplicationException("Already has putback item");
            }

            _hasPutBackItem = true;
        }

        public T Current => _enumerator.Current;
    }
}