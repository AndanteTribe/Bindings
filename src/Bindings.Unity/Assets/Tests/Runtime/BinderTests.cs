#nullable enable

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Bindings.Internal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Bindings.Tests
{
    public sealed class BinderTests
    {
        private Binder _binder = null!;
        private RecordingView _view = null!;

        [SetUp]
        public void SetUp()
        {
            _view = new RecordingView();
            _binder = TestObjects.CreateBinder(_view);
        }

        [TearDown]
        public void TearDown()
        {
            if (_binder != null)
            {
                Object.DestroyImmediate(_binder.gameObject);
            }
        }

        [Test]
        public void InitializeAssignsEveryCompatibleViewAndRejectsMissingModels()
        {
            var second = new RecordingView();
            TestObjects.SetField(_binder, "_views", new IView[] { _view, second });
            var model = new TestViewModel();
            _binder.Initialize(new IViewModel[] { model });
            Assert.That(_view.Model, Is.SameAs(model));
            Assert.That(second.Model, Is.SameAs(model));
            _ = Assert.Throws<InvalidOperationException>(() => _binder.Initialize(new OtherViewModel()));
            _ = Assert.Throws<InvalidOperationException>(() => ((IView)_view).Initialize(new OtherViewModel()));
            Assert.That(((IView)_view).CanBind(new OtherViewModel()), Is.False);
        }

        [Test]
        public void RunCancelsPreviousBindingsAndDestroyCancelsCurrentBindings()
        {
            _binder.Run();
            var previous = _view.Token;
            Assert.That(previous.IsCancellationRequested, Is.False);
            _binder.Run();
            Assert.That(previous.IsCancellationRequested, Is.True);
            Assert.That(_view.Token.IsCancellationRequested, Is.False);
            Assert.That(_view.BindCount, Is.EqualTo(2));
            Object.DestroyImmediate(_binder.gameObject);
            Assert.That(_view.Token.IsCancellationRequested, Is.True);
        }

        [UnityTest]
        public IEnumerator RunOnStartBindsOnce() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            Object.DestroyImmediate(_binder.gameObject);
            _binder = TestObjects.CreateBinder(_view);
            TestObjects.SetField(_binder, "_runOnStart", true);
            await Awaitable.NextFrameAsync(cancellationToken);
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(_view.BindCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator RebindRequestsAreDeferredAndCoalesced() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            var publisher = (IMvvmPublisher)_binder;
            publisher.PublishRebindMessage<TestViewModel>();
            Assert.That(_view.BindCount, Is.Zero);
            _binder.Run();
            publisher.PublishRebindMessage<TestViewModel>();
            publisher.PublishRebindMessage<TestViewModel>();
            Assert.That(_view.BindCount, Is.EqualTo(1));
            Canvas.ForceUpdateCanvases();
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(_view.BindCount, Is.EqualTo(2));
            publisher.PublishRebindMessage<TestViewModel>();
            Canvas.ForceUpdateCanvases();
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(_view.BindCount, Is.EqualTo(3));
            _ = Assert.Throws<InvalidOperationException>(() => publisher.PublishRebindMessage<OtherViewModel>());
        });

        [UnityTest]
        public IEnumerator RestartRemovesCancelledRequestsFromTheScheduler() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            _binder.Run();
            ((IMvvmPublisher)_binder).PublishRebindMessage<TestViewModel>();
            _binder.Run();
            Canvas.ForceUpdateCanvases();
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(_view.BindCount, Is.EqualTo(2));
            ((IMvvmPublisher)_binder).PublishRebindMessage<TestViewModel>();
            Canvas.ForceUpdateCanvases();
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(_view.BindCount, Is.EqualTo(3));
        });

        [Test]
        public void MessagesReachSyncAndAsyncSubscribers()
        {
            var asyncView = new AsyncView();
            TestObjects.SetField(_binder, "_views", new IView[] { _view, asyncView });
            ((IMvvmPublisher)_binder).Publish(42);
            Assert.That(_view.Message, Is.EqualTo(42));
            Assert.That(asyncView.Message, Is.EqualTo(42));
            Assert.That(asyncView.Token.CanBeCanceled, Is.True);
        }

        [UnityTest]
        public IEnumerator ItemPublisherRoutesOnlyToItsCurrentLivingBinder() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            var model = new TestViewModel();
            var publisher = CreateItemPublisher(model);
            ((IMvvmPublisher)publisher).Publish(1);
            ((IMvvmPublisher)publisher).PublishRebindMessage<TestViewModel>();
            publisher.Binder = _binder;
            Assert.That(_view.Model, Is.SameAs(model));
            Assert.That(_view.BindCount, Is.EqualTo(1));
            ((IMvvmPublisher)publisher).Publish(9);
            Assert.That(_view.Message, Is.EqualTo(9));
            ((IMvvmPublisher)publisher).PublishRebindMessage<TestViewModel>();
            Canvas.ForceUpdateCanvases();
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(_view.BindCount, Is.EqualTo(2));
            publisher.Binder = null;
            ((IMvvmPublisher)publisher).Publish(10);
            Assert.That(_view.Message, Is.EqualTo(9));
            publisher.Binder = _binder;
            Object.DestroyImmediate(_binder.gameObject);
            Assert.That(publisher.Binder, Is.Null);
            Assert.DoesNotThrow(() => ((IMvvmPublisher)publisher).Publish(11));
        });

        private static ScrollItemMvvmPublisher CreateItemPublisher(TestViewModel model)
        {
            var publisher = ScrollItemMvvmPublisher.Create(out var container);
            container.Register(model);
            return publisher;
        }

        [Test]
        public void DebugMessageOnlyInitializesMatchingViews()
        {
            new DebugBindMessage(new OtherViewModel()).BindTo(_view);
            Assert.That(_view.Model, Is.Null);
            var model = new TestViewModel();
            new DebugBindMessage(model).BindTo(_view);
            Assert.That(_view.Model, Is.SameAs(model));
        }

        [Test]
        public void SchedulerRejectsAlreadyCancelledTokens()
        {
            _ = Assert.Throws<OperationCanceledException>(() => BindingScheduler.EnqueueAsync(new CancellationToken(true)));
        }

        private sealed class AsyncView : IView<TestViewModel>, IAsyncMvvmSubscriber<int>
        {
            public int Message;
            public CancellationToken Token;

            public void Initialize(TestViewModel viewModel) { }
            public ValueTask BindAsync(CancellationToken cancellationToken) => default;

            public ValueTask OnReceivedMessageAsync(int message, CancellationToken cancellationToken)
            {
                Message = message;
                Token = cancellationToken;
                return default;
            }
        }
    }
}