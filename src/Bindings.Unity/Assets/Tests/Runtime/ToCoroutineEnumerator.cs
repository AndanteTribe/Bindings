#nullable enable

using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Bindings.Tests
{
    internal sealed class ToCoroutineEnumerator : IEnumerator, IDisposable
    {
        private readonly Func<CancellationToken, ValueTask> _task;
        private readonly CancellationTokenSource _cancellation = new();
        private bool _started;
        private bool _disposed;
        private volatile bool _completed;
        private ExceptionDispatchInfo? _exception;

        public ToCoroutineEnumerator(Func<CancellationToken, ValueTask> task) => _task = task;

        public object Current => null!;

        public bool MoveNext()
        {
            if (_disposed)
            {
                return false;
            }
            if (!_started)
            {
                _started = true;
                _ = RunTask();
            }
            if (_completed)
            {
                Dispose();
                _exception?.Throw();
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            try
            {
                // Test bodies must pass this token to their asynchronous operations.
                if (!_completed)
                {
                    _cancellation.Cancel();
                }
            }
            finally
            {
                _cancellation.Dispose();
            }
        }

        void IEnumerator.Reset() => throw new NotSupportedException("Reset is not supported for this enumerator.");

        private async ValueTask RunTask()
        {
            try
            {
                await _task(_cancellation.Token);
            }
            catch (Exception exception)
            {
                _exception = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                _completed = true;
            }
        }
    }
}