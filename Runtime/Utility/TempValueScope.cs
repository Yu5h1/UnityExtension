using System;
using System.Collections.Generic;
using System.Text;

namespace Yu5h1Lib
{
    public readonly struct TempValueScope<T> : IDisposable
    {
        private readonly T _previousValue;
        private readonly Action<T> _setter;

        public TempValueScope(Func<T> getter, Action<T> setter, T newValue)
        {
            _previousValue = getter();
            _setter = setter;
            setter(newValue);
        }

        public void Dispose() => _setter(_previousValue);
    }
}
