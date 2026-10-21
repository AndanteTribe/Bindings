#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Bindings.Internal
{
    internal sealed class DisposableBag : IDisposable
    {
        private IDisposable[] _disposables = Array.Empty<IDisposable>();
        private int _count;

        public void Add(IDisposable disposable)
        {
            if (_count == _disposables.Length)
            {
                var array = ArrayPool<IDisposable>.Shared.Rent(_count + 1);
                _disposables.AsSpan().CopyTo(array);
                if (_disposables.Length > 0)
                {
                    ArrayPool<IDisposable>.Shared.Return(_disposables, clearArray: true);
                }
                _disposables = array;
            }
            _disposables[_count++] = disposable;
        }

        public void Dispose()
        {
            var disposables = _disposables;
            var count = _count;
            _disposables = Array.Empty<IDisposable>();
            _count = 0;
            List<Exception>? exceptions = null;
            for (var i = count - 1; i >= 0; i--)
            {
                try
                {
                    disposables[i].Dispose();
                }
                catch (Exception exception)
                {
                    (exceptions ??= new List<Exception>()).Add(exception);
                }
            }
            if (disposables.Length > 0)
            {
                ArrayPool<IDisposable>.Shared.Return(disposables, clearArray: true);
            }
            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}