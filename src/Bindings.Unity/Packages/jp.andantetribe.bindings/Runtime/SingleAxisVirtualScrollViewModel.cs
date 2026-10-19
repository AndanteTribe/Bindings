#nullable enable

using System;

namespace Bindings
{
    public sealed class SingleAxisVirtualScrollViewModel : IViewModel
    {
        public readonly ReadOnlyMemory<ScrollItemMvvmPublisher> Publishers;
        private float _scrollRatio;
        private readonly IMvvmPublisher _publisher;

        public SingleAxisVirtualScrollViewModel(
            ReadOnlyMemory<ScrollItemMvvmPublisher> publishers, IMvvmPublisher publisher)
        {
            _publisher = publisher;
            Publishers = publishers;
        }

        public float ScrollRatio
        {
            get => _scrollRatio;
            set
            {
                if (float.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _scrollRatio = Math.Clamp(value, 0, 1);
                _publisher.Publish(new ScrollToRatioMessage(_scrollRatio));
            }
        }
    }
}