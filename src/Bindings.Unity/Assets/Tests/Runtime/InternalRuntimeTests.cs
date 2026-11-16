#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Bindings.Internal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bindings.Tests
{
    public sealed class InternalRuntimeTests
    {
        [Test]
        public void DisposableBagGrowsDisposesInReverseAndCanBeReused()
        {
            var disposed = new List<int>();
            var bag = new DisposableBag();
            bag.Dispose();
            for (var i = 0; i < 100; i++)
            {
                var index = i;
                bag.Add(new CallbackDisposable(() => disposed.Add(index)));
            }
            bag.Dispose();
            Assert.That(disposed.Count, Is.EqualTo(100));
            for (var i = 0; i < 100; i++)
            {
                Assert.That(disposed[i], Is.EqualTo(99 - i));
            }
            bag.Dispose();
            Assert.That(disposed.Count, Is.EqualTo(100));
            bag.Add(new CallbackDisposable(() => disposed.Add(100)));
            bag.Dispose();
            Assert.That(disposed[100], Is.EqualTo(100));
        }

        [Test]
        public void DisposableBagAggregatesFailuresAndStillDisposesEveryEntry()
        {
            var bag = new DisposableBag();
            var disposed = false;
            bag.Add(new CallbackDisposable(() => throw new ArgumentException("first")));
            bag.Add(new CallbackDisposable(() => disposed = true));
            bag.Add(new CallbackDisposable(() => throw new InvalidOperationException("last")));
            var error = Assert.Throws<AggregateException>(bag.Dispose)!;
            Assert.That(disposed, Is.True);
            Assert.That(error.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(error.InnerExceptions[0].Message, Is.EqualTo("last"));
            Assert.That(error.InnerExceptions[1].Message, Is.EqualTo("first"));
            Assert.DoesNotThrow(bag.Dispose);
        }

        [Test]
        public void ForgetConsumesCompletedSuccessCancellationAndFailure()
        {
            default(ValueTask).Forget();
            new ValueTask(Task.FromCanceled(new CancellationToken(true))).Forget();
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: synchronous"));
            new ValueTask(Task.FromException(new InvalidOperationException("synchronous"))).Forget();
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: source"));
            new ValueTask(new ThrowingSource(), 0).Forget();
        }

        [Test]
        public void ForgetConsumesDeferredOperationsAndReusesContinuationState()
        {
            var success = new TaskCompletionSource<bool>();
            new ValueTask(success.Task).Forget();
            success.SetResult(true);
            var cancelled = new TaskCompletionSource<bool>();
            new ValueTask(cancelled.Task).Forget();
            cancelled.SetCanceled();
            var failed = new TaskCompletionSource<bool>();
            new ValueTask(failed.Task).Forget();
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: deferred"));
            failed.SetException(new InvalidOperationException("deferred"));
        }

        [Test]
        public void BindingSourceCompletesCancelsAndInvalidatesOldVersionsWhenReused()
        {
            var source = BindingValueSource.Create();
            var version = source.Version;
            var task = new ValueTask(source, version);
            Assert.That(task.IsCompleted, Is.False);
            var completed = false;
            ((IValueTaskSource)source).OnCompleted(_ => completed = true, null, version, ValueTaskSourceOnCompletedFlags.None);
            source.SetResult();
            Assert.That(completed, Is.True);
            task.GetAwaiter().GetResult();
            var reused = BindingValueSource.Create();
            Assert.That(reused, Is.SameAs(source));
            Assert.That(reused.Version, Is.Not.EqualTo(version));
            _ = Assert.Throws<InvalidOperationException>(() => ((IValueTaskSource)source).GetStatus(version));
            using var cancellation = new CancellationTokenSource();
            reused.Registration = cancellation.Token.Register(() => { });
            var cancelledTask = new ValueTask(reused, reused.Version);
            reused.SetCancel();
            Assert.That(cancelledTask.IsCanceled, Is.True);
            _ = Assert.Throws<OperationCanceledException>(() => cancelledTask.GetAwaiter().GetResult());
        }

        [Test]
        public void SuccessfulSourceCannotBeCancelledBeforeItsResultIsConsumed()
        {
            using var cancellation = new CancellationTokenSource();
            var source = BindingValueSource.Create();
            source.Registration = cancellation.Token.Register(source.SetCancel);
            var task = new ValueTask(source, source.Version);
            source.SetResult();
            Assert.DoesNotThrow(cancellation.Cancel);
            Assert.That(task.IsCompletedSuccessfully, Is.True);
            Assert.DoesNotThrow(() => task.GetAwaiter().GetResult());
        }

        private sealed class CallbackDisposable : IDisposable
        {
            private readonly Action _callback;

            public CallbackDisposable(Action callback) => _callback = callback;
            public void Dispose() => _callback();
        }

        private sealed class ThrowingSource : IValueTaskSource
        {
            public void GetResult(short token) => throw new InvalidOperationException("source");
            public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Succeeded;
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => throw new NotSupportedException();
        }
    }

}