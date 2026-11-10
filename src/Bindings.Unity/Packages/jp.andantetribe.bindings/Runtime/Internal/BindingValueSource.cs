#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        public CancellationTokenRegistration Registration { get; set; }

        public short Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _core.Version;
        }

        private BindingValueSource()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BindingValueSource Create()
        {
            if (s_head == null)
            {
                return new BindingValueSource();
            }

            var instance = s_head;
            s_head = instance._next;
            instance._next = null;
            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult() => _core.SetResult(Unit.Default);

        public void SetCancel() => _core.SetException(new OperationCanceledException(Registration.Token));

        [DebuggerNonUserCode]
        void IValueTaskSource.GetResult(short token)
        {
            try
            {
                _ = _core.GetResult(token);
            }
            finally
            {
                Reset();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);

        private void Reset()
        {
            Registration.Dispose();
            Registration = default;
            _core.Reset();
            _next = s_head;
            s_head = this;
        }
    }
}