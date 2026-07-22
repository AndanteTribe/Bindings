#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace Bindings.Internal
{
    internal sealed class BindingValueSource : IValueTaskSource
    {
        private static BindingValueSource? s_head;
        private BindingValueSource? _next;

        private ManualResetValueTaskSourceCore<Unit> _core = new()
        {
            RunContinuationsAsynchronously = false
        };
        private CancellationToken _cancellationToken;

        public short Version => _core.Version;

        private BindingValueSource()
        {
        }

        public static BindingValueSource Create(CancellationToken cancellationToken)
        {
            if (s_head == null)
            {
                return new BindingValueSource
                {
                    _cancellationToken = cancellationToken
                };
            }

            var instance = s_head;
            s_head = instance._next;
            instance._next = null;
            instance._cancellationToken = cancellationToken;
            return instance;
        }

        public void SetResult() => _core.SetResult(Unit.Default);

        public void SetCancel() => _core.SetException(new OperationCanceledException(_cancellationToken));

        [DebuggerNonUserCode]
        void IValueTaskSource.GetResult(short token)
        {
            try
            {
                _core.GetResult(token);
            }
            finally
            {
                Reset();
            }
        }

        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);

        void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);

        private void Reset()
        {
            _core.Reset();
            _next = s_head;
            s_head = this;
        }
    }
}