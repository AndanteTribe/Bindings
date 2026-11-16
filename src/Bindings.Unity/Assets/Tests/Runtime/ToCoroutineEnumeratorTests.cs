#nullable enable

using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bindings.Tests
{
    public sealed class ToCoroutineEnumeratorTests
    {
        [Test]
        public void CompletedBodyRunsOnceAndDoesNotYield()
        {
            var calls = 0;
            using var enumerator = new ToCoroutineEnumerator(_ =>
            {
                calls++;
                return default;
            });
            Assert.That(calls, Is.Zero);
            Assert.That(enumerator.MoveNext(), Is.False);
            Assert.That(enumerator.MoveNext(), Is.False);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void SynchronousFailureIsRethrownAsTheOriginalException()
        {
            var expected = new AssertionException("synchronous assertion");
            using var enumerator = new ToCoroutineEnumerator(_ => throw expected);
            var actual = Assert.Throws<AssertionException>(() => enumerator.MoveNext());
            Assert.That(actual, Is.SameAs(expected));
        }

        [UnityTest]
        public IEnumerator DeferredBodyYieldsUntilCompletion() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            var completion = new TaskCompletionSource<bool>();
            using var enumerator = new ToCoroutineEnumerator(_ => new ValueTask(completion.Task));
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.Null);
            Assert.That(enumerator.MoveNext(), Is.True);
            completion.SetResult(true);
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(enumerator.MoveNext(), Is.False);
        });

        [UnityTest]
        public IEnumerator DeferredFailureReachesTheEnumerator() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            var completion = new TaskCompletionSource<bool>();
            var expected = new AssertionException("deferred assertion");
            using var enumerator = new ToCoroutineEnumerator(_ => new ValueTask(completion.Task));
            Assert.That(enumerator.MoveNext(), Is.True);
            completion.SetException(expected);
            await Awaitable.NextFrameAsync(cancellationToken);
            var actual = Assert.Throws<AssertionException>(() => enumerator.MoveNext());
            Assert.That(actual, Is.SameAs(expected));
        });

        [Test]
        public void DisposingAnInterruptedTestCancelsItsPendingOperation()
        {
            Task? pending = null;
            using var enumerator = new ToCoroutineEnumerator(token =>
            {
                pending = Task.Delay(Timeout.Infinite, token);
                return new ValueTask(pending);
            });
            Assert.That(enumerator.MoveNext(), Is.True);
            enumerator.Dispose();
            Assert.That(pending!.IsCanceled, Is.True);
            Assert.That(enumerator.MoveNext(), Is.False);
        }
    }
}