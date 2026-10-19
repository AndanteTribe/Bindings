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
        valueTaskSource.SetException(new OperationCanceledException());

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
        private ManualResetValueTaskSourceCore<bool> _source;

        public TestValueTaskSource()
        {
            _source.RunContinuationsAsynchronously = true;
        }

        public Task GetResultCalled => _getResultCalled.Task;

        public ValueTask CreateValueTask() => new(this, _source.Version);

        public void SetException(Exception exception) => _source.SetException(exception);

        public void GetResult(short token)
        {
            try
            {
                _source.GetResult(token);
            }
            finally
            {
                _getResultCalled.TrySetResult(true);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _source.OnCompleted(continuation, state, token, flags);
    }
}
