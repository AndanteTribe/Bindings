#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bindings.Internal
{
    internal sealed class DisposableBag : IDisposable
    {
        private IDisposable[] _disposables = Array.Empty<IDisposable>();
        private int _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(IDisposable disposable)
        {
            if (_count == _disposables.Length)
            {
                GrowCore(ref _disposables, _count);
            }
            _disposables[_count++] = disposable;

            static void GrowCore(ref IDisposable[] disposables, int count)
            {
                var array = ArrayPool<IDisposable>.Shared.Rent(count + 1);
                disposables.AsSpan().CopyTo(array);
                if (disposables.Length > 0)
                {
                    ArrayPool<IDisposable>.Shared.Return(disposables, clearArray: true);
                }
                disposables = array;
            }
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