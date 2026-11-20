#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Bindings.Tests
{
    public sealed class VirtualScrollTests
    {
        private GameObject _root = null!;
        private Binder _prefab = null!;
        private ScrollRect _scroll = null!;
        private SingleAxisVirtualScrollView _view = null!;
        private SingleAxisVirtualScrollViewModel _model = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Scroll", typeof(RectTransform), typeof(Canvas));
            _scroll = _root.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.inertia = false;
            _scroll.viewport = Rect("Viewport", _root.transform, new Vector2(120, 100));
            _scroll.content = Rect("Content", _scroll.viewport, new Vector2(120, 100));
            _prefab = TestObjects.CreateBinder();
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(20, 20);
            _view = new SingleAxisVirtualScrollView();
            TestObjects.SetField(_view, "_itemPrefab", _prefab);
            TestObjects.SetField(_view, "_scrollRect", _scroll);
            SetModel(20);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_prefab.gameObject);
        }

        [TestCase(-1, 0)]
        [TestCase(0, 0)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(2, 1)]
        [TestCase(float.NegativeInfinity, 0)]
        [TestCase(float.PositiveInfinity, 1)]
        public void RatioClampsAndPublishes(float input, float expected)
        {
            var publisher = new RecordingPublisher();
            var model = new SingleAxisVirtualScrollViewModel(default, publisher) { ScrollRatio = input };
            Assert.That(model.ScrollRatio, Is.EqualTo(expected));
            Assert.That(((SingleAxisVirtualScrollViewModel.ScrollToRatioMessage)publisher.Messages.Single()).Ratio,
                Is.EqualTo(expected));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => model.ScrollRatio = float.NaN);
            Assert.That(publisher.Messages.Count, Is.EqualTo(1));
            Assert.That(model.ScrollRatio, Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator VerticalListReusesItemsAndDetachesOffscreenPublishers() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            await ((IView)_view).BindAsync(cancellationToken);
            Assert.That(_scroll.content.rect.height, Is.EqualTo(400));
            Assert.That(_scroll.content.childCount, Is.EqualTo(6));
            var original = _scroll.content.GetComponentsInChildren<Binder>(true);
            Assert.That(_model._publishers.Span[0].Binder, Is.Not.Null);
            ScrollTo(1);
            Assert.That(_model.ScrollRatio, Is.EqualTo(1).Within(0.001));
            Assert.That(_model._publishers.Span[0].Binder, Is.Null);
            Assert.That(_model._publishers.Span[19].Binder, Is.Not.Null);
            CollectionAssert.AreEquivalent(original, _scroll.content.GetComponentsInChildren<Binder>(true));
            ScrollTo(1);
            ScrollTo(0.5f);
            ScrollTo(0);
            Assert.That(_model._publishers.Span[0].Binder, Is.Not.Null);
            Assert.That(_model._publishers.Span[19].Binder, Is.Null);
        });

        [UnityTest]
        public IEnumerator HorizontalGridSupportsPartialFinalLaneAndSpacing() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            _scroll.horizontal = true;
            _scroll.vertical = false;
            TestObjects.SetField(_view, "_laneCount", 3);
            TestObjects.SetField(_view, "_spacing", 2f);
            TestObjects.SetField(_view, "_laneSpacing", 4f);
            SetModel(31);
            _model.ScrollRatio = 1;
            await ((IView)_view).BindAsync(cancellationToken);
            Assert.That(_scroll.content.rect.width, Is.EqualTo(240));
            Assert.That(_model._publishers.Span[30].Binder, Is.Not.Null);
            Assert.That(_model._publishers.Span[0].Binder, Is.Null);
            ScrollTo(0);
            var first = _model._publishers.Span[0].Binder!;
            var second = _model._publishers.Span[1].Binder!;
            Assert.That(first.transform.localPosition.y - second.transform.localPosition.y, Is.EqualTo(24).Within(0.001));
            Assert.That(_model.ScrollRatio, Is.Zero.Within(0.001));
        });

        [UnityTest]
        public IEnumerator RebindGrowsShrinksAndClearsOldBindings() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            SetModel(1);
            await ((IView)_view).BindAsync(cancellationToken);
            Assert.That(_scroll.content.childCount, Is.EqualTo(1));
            var old = _model;
            SetModel(20);
            await ((IView)_view).BindAsync(cancellationToken);
            Assert.That(old._publishers.Span[0].Binder, Is.Null);
            Assert.That(_scroll.content.childCount, Is.EqualTo(6));
            ScrollTo(1);
            old = _model;
            SetModel(2);
            await ((IView)_view).BindAsync(cancellationToken);
            Assert.That(old._publishers.ToArray().All(p => p.Binder is null), Is.True);
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(_scroll.content.childCount, Is.EqualTo(2));
            old = _model;
            SetModel(0);
            await ((IView)_view).BindAsync(cancellationToken);
            await Awaitable.NextFrameAsync(cancellationToken);
            Assert.That(old._publishers.Span[0].Binder, Is.Null);
            Assert.That(_scroll.content.childCount, Is.Zero);
            Assert.That(_scroll.content.rect.height, Is.Zero);
            Assert.DoesNotThrow(() => ScrollTo(1));
        });

        [UnityTest]
        public IEnumerator InvalidConfigurationAndCancellationFailBeforeInstantiation() => new ToCoroutineEnumerator(async cancellationToken =>
        {
            var cancelled = ((IView)_view).BindAsync(new CancellationToken(true)).AsTask();
            _ = Assert.Throws<OperationCanceledException>(() => cancelled.GetAwaiter().GetResult());
            _scroll.horizontal = true;
            var bothAxes = ((IView)_view).BindAsync(default).AsTask();
            _ = Assert.Throws<InvalidOperationException>(() => bothAxes.GetAwaiter().GetResult());
            _scroll.horizontal = false;
            _scroll.vertical = false;
            var noAxis = ((IView)_view).BindAsync(default).AsTask();
            _ = Assert.Throws<InvalidOperationException>(() => noAxis.GetAwaiter().GetResult());
            _scroll.vertical = true;
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(20, 0);
            var zeroHeight = ((IView)_view).BindAsync(default).AsTask();
            _ = Assert.Throws<InvalidOperationException>(() => zeroHeight.GetAwaiter().GetResult());
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(0, 20);
            TestObjects.SetField(_view, "_laneCount", 2);
            var zeroWidth = ((IView)_view).BindAsync(default).AsTask();
            _ = Assert.Throws<InvalidOperationException>(() => zeroWidth.GetAwaiter().GetResult());
            Assert.That(_scroll.content.childCount, Is.Zero);
            await Awaitable.NextFrameAsync(cancellationToken);
        });

        private void SetModel(int count)
        {
            var publishers = new ScrollItemMvvmPublisher[count];
            for (var i = 0; i < count; i++)
            {
                publishers[i] = ScrollItemMvvmPublisher.Create(out _);
            }
            _model = new SingleAxisVirtualScrollViewModel(publishers, new RecordingPublisher());
            ((IView<SingleAxisVirtualScrollViewModel>)_view).Initialize(_model);
        }

        private void ScrollTo(float ratio)
        {
            ((IMvvmSubscriber<SingleAxisVirtualScrollViewModel.ScrollToRatioMessage>)_view)
                .OnReceivedMessage(new SingleAxisVirtualScrollViewModel.ScrollToRatioMessage(ratio));
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = size;
            return rect;
        }
    }
}