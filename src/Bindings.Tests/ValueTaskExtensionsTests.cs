using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Bindings.Internal;
using UnityEngine;
using Xunit;

namespace Bindings.Tests;

public class ValueTaskExtensionsTests
{
    [Fact]
    public void Forget_CompletedCanceledValueTask_DoesNotLog()
    {
        Debug.Reset();

        ValueTask.FromCanceled(new CancellationToken(canceled: true)).Forget();

        Assert.Empty(Debug.LoggedExceptions);
    }

    [Fact]
    public async Task Forget_AsynchronouslyCanceledValueTask_DoesNotLog()
    {
        Debug.Reset();
        var valueTaskSource = new TestValueTaskSource();

        valueTaskSource.CreateValueTask().Forget();
        valueTaskSource.SetCanceled(new CancellationToken(canceled: true));

        await valueTaskSource.GetResultCalled;

        Assert.Empty(Debug.LoggedExceptions);
    }

    [Fact]
    public void Forget_CompletedFaultedValueTask_LogsExceptionInDebugBuild()
    {
        Debug.Reset();
        var exception = new InvalidOperationException("boom");

        ValueTask.FromException(exception).Forget();

        var logged = Assert.Single(Debug.LoggedExceptions);
        Assert.Same(exception, logged);
    }

    [Fact]
    public async Task Forget_AsynchronouslyFaultedValueTask_LogsExceptionInDebugBuild()
    {
        Debug.Reset();
        var exception = new InvalidOperationException("boom");
        var valueTaskSource = new TestValueTaskSource();

        valueTaskSource.CreateValueTask().Forget();
        valueTaskSource.SetException(exception);

        await valueTaskSource.GetResultCalled;

        var logged = Assert.Single(Debug.LoggedExceptions);
        Assert.Same(exception, logged);
    }

    private sealed class TestValueTaskSource : IValueTaskSource
    {
        private readonly TaskCompletionSource<bool> _getResultCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Exception? _exception;
        private Action<object?>? _continuation;
        private object? _state;
        private ValueTaskSourceStatus _status;

        public Task GetResultCalled => _getResultCalled.Task;

        public ValueTask CreateValueTask() => new(this, token: 0);

        public void SetCanceled(CancellationToken cancellationToken)
        {
            _exception = new OperationCanceledException(cancellationToken);
            _status = ValueTaskSourceStatus.Canceled;
            InvokeContinuationIfRegistered();
        }

        public void SetException(Exception exception)
        {
            _exception = exception;
            _status = ValueTaskSourceStatus.Faulted;
            InvokeContinuationIfRegistered();
        }

        public void GetResult(short token)
        {
            try
            {
                switch (_status)
                {
                    case ValueTaskSourceStatus.Canceled:
                    case ValueTaskSourceStatus.Faulted:
                        throw _exception!;
                }
            }
            finally
            {
                _getResultCalled.TrySetResult(true);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _status;

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (_status != ValueTaskSourceStatus.Pending)
            {
                continuation(state);
                return;
            }

            _continuation = continuation;
            _state = state;
        }

        private void InvokeContinuationIfRegistered()
        {
            var continuation = _continuation;
            if (continuation == null)
            {
                return;
            }

            var state = _state;
            _continuation = null;
            _state = null;
            continuation(state);
        }
    }
}
