#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Bindings
{
    /// <summary>
    /// Displays a single-axis list by reusing item Binders as items enter and leave the viewport.
    /// </summary>
    /// <remarks>
    /// Add this view to a Binder and configure its item Binder prefab, spacing, and ScrollRect in the Inspector.
    /// Enable exactly one ScrollRect direction and use a prefab with a positive size along that axis.
    /// </remarks>
    [Serializable]
    public sealed class SingleAxisVirtualScrollView : IView<SingleAxisVirtualScrollViewModel>, IMvvmSubscriber<SingleAxisVirtualScrollViewModel.ScrollToRatioMessage>
    {
        [SerializeField]
        private Binder _itemPrefab = null!;
        [SerializeField, Min(0)]
        private float _spacing;
        [SerializeField]
        private ScrollRect _scrollRect = null!;

        private SingleAxisVirtualScrollViewModel _viewModel = null!;
        private ReadOnlyMemory<Binder> _items = ReadOnlyMemory<Binder>.Empty;
        private ReadOnlyMemory<ScrollItemMvvmPublisher> _boundPublishers = ReadOnlyMemory<ScrollItemMvvmPublisher>.Empty;
        private RectTransform.Axis _axis;
        private float _itemSize;
        private int _visibleStart;
        private int _visibleCount;
        private int _firstIndex = -1;

        void IView<SingleAxisVirtualScrollViewModel>.Initialize(SingleAxisVirtualScrollViewModel viewModel) => _viewModel = viewModel;

        async ValueTask IView.BindAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var viewModel = _viewModel;
            var publishers = viewModel._publishers;
            if (_scrollRect.vertical == _scrollRect.horizontal)
            {
                throw new InvalidOperationException("Enable exactly one ScrollRect direction: Vertical or Horizontal.");
            }
            _axis = _scrollRect.vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal;
            _itemSize = ((RectTransform)_itemPrefab.transform).rect.size[(int)_axis];
            if (_itemSize <= 0)
            {
                throw new InvalidOperationException("The scroll item prefab must have a positive size along the scroll axis.");
            }

            ResetBindings();
            var pitch = _itemSize + _spacing;
            var count = Mathf.Min(publishers.Length, Mathf.CeilToInt(_scrollRect.viewport.rect.size[(int)_axis] / pitch) + 1);
            var content = _scrollRect.content;
            var length = Mathf.Max(0, publishers.Length * pitch - _spacing);
            content.SetSizeWithCurrentAnchors(_axis, length);

            await ResizeItemsAsync(count, cancellationToken);
            _boundPublishers = publishers;
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            ScrollTo(viewModel.ScrollRatio);
        }

        void IMvvmSubscriber<SingleAxisVirtualScrollViewModel.ScrollToRatioMessage>.OnReceivedMessage(
            SingleAxisVirtualScrollViewModel.ScrollToRatioMessage message)
        {
            if (!_boundPublishers.IsEmpty)
            {
                ScrollTo(message.Ratio);
            }
        }

        private void ScrollTo(float ratio)
        {
            _scrollRect.StopMovement();
            if (_axis == RectTransform.Axis.Vertical)
            {
                _scrollRect.verticalNormalizedPosition = 1 - ratio;
            }
            else
            {
                _scrollRect.horizontalNormalizedPosition = ratio;
            }
            OnScrollValueChanged(_scrollRect.normalizedPosition);
        }

        private async ValueTask ResizeItemsAsync(int count, CancellationToken cancellationToken)
        {
            if (_items.Length > count)
            {
                TrimItems(count);
                void TrimItems(int c)
                {
                    foreach (var item in _items[c..].Span)
                    {
                        UnityEngine.Object.Destroy(item.gameObject);
                    }
                    _items = _items[..c];
                }
            }
            else if (_items.Length < count)
            {
                var addedItems = await UnityEngine.Object.InstantiateAsync(
                    _itemPrefab,
                    count - _items.Length,
                    new InstantiateParameters { parent = _scrollRect.content, originalImmutable = true },
                    cancellationToken);
                foreach (var item in addedItems)
                {
                    item.gameObject.SetActive(false);
                }
                if (_items.IsEmpty)
                {
                    _items = addedItems;
                }
                else
                {
                    var items = new Binder[count];
                    _items.CopyTo(items);
                    addedItems.AsSpan().CopyTo(items.AsSpan(_items.Length));
                    _items = items;
                }
            }
        }

        private void OnScrollValueChanged(Vector2 position)
        {
            _viewModel._scrollRatio = Mathf.Clamp01(_axis == RectTransform.Axis.Vertical ? 1 - position.y : position.x);

            var items = _items.Span;
            if (items.IsEmpty)
            {
                return;
            }

            var viewport = _scrollRect.viewport;
            var contentRect = _scrollRect.content.rect;
            var viewportStart = _scrollRect.content.InverseTransformPoint(
                viewport.TransformPoint(new Vector3(viewport.rect.xMin, viewport.rect.yMax)));
            var offset = _axis == RectTransform.Axis.Vertical ? contentRect.yMax - viewportStart.y : viewportStart.x - contentRect.xMin;
            var length = viewport.rect.size[(int)_axis];
            var pitch = _itemSize + _spacing;
            var first = Mathf.Clamp(Mathf.FloorToInt((offset - _itemSize) / pitch) + 1, 0, _boundPublishers.Length);
            var end = Mathf.Clamp(Mathf.CeilToInt((offset + length) / pitch), first, _boundPublishers.Length);
            var count = end - first;
            if (_firstIndex == first && _visibleCount == count)
            {
                return;
            }

            var start = first % items.Length;
            var previousFirst = _firstIndex;
            var previousEnd = previousFirst + _visibleCount;
            for (var i = 0; i < _visibleCount; i++)
            {
                var index = previousFirst + i;
                if (index >= first && index < end)
                {
                    continue;
                }
                _boundPublishers.Span[index].Binder = null;
                var slot = (_visibleStart + i) % items.Length;
                if ((slot - start + items.Length) % items.Length >= count)
                {
                    items[slot].gameObject.SetActive(false);
                }
            }
            _firstIndex = first;
            _visibleStart = start;
            _visibleCount = count;

            for (var i = 0; i < _visibleCount; i++)
            {
                var index = first + i;
                if (index >= previousFirst && index < previousEnd)
                {
                    continue;
                }
                var item = items[(_visibleStart + i) % items.Length];
                var rect = (RectTransform)item.transform;
                rect.localPosition = _axis == RectTransform.Axis.Vertical
                    ? new Vector3(
                        contentRect.center.x + (rect.pivot.x - 0.5f) * rect.rect.width,
                        contentRect.yMax - index * pitch - (1 - rect.pivot.y) * _itemSize, 0)
                    : new Vector3(
                        contentRect.xMin + index * pitch + rect.pivot.x * _itemSize,
                        contentRect.center.y + (rect.pivot.y - 0.5f) * rect.rect.height, 0);
                _boundPublishers.Span[index].Binder = item;
                item.gameObject.SetActive(true);
            }
        }

        private void ResetBindings()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            foreach (var publisher in _boundPublishers.Span)
            {
                publisher.Binder = null;
            }
            foreach (var item in _items.Span)
            {
                item.gameObject.SetActive(false);
            }
            _boundPublishers = ReadOnlyMemory<ScrollItemMvvmPublisher>.Empty;
            _visibleStart = 0;
            _visibleCount = 0;
            _firstIndex = -1;
        }
    }
}