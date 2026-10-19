#nullable enable

using System;

namespace Bindings
{
    /// <summary>
    /// Provides item publishers and scroll position requests for a single-axis virtual scroll view.
    /// </summary>
    public sealed class SingleAxisVirtualScrollViewModel : IViewModel
    {
        /// <summary>
        /// The publishers for the scroll items, in display order.
        /// </summary>
        public readonly ReadOnlyMemory<ScrollItemMvvmPublisher> Publishers;
        private float _scrollRatio;
        private readonly IMvvmPublisher _publisher;

        /// <summary>
        /// Creates a ViewModel with the supplied item publishers and scroll message publisher.
        /// </summary>
        /// <param name="publishers">The item publishers in display order.</param>
        /// <param name="publisher">The publisher that routes scroll requests to the view.</param>
        public SingleAxisVirtualScrollViewModel(
            ReadOnlyMemory<ScrollItemMvvmPublisher> publishers, IMvvmPublisher publisher)
        {
            _publisher = publisher;
            Publishers = publishers;
        }

        /// <summary>
        /// Gets the last requested scroll ratio or requests a new position, clamped to the range from zero to one.
        /// Zero represents the top or left edge; one represents the bottom or right edge.
        /// </summary>
        /// <remarks>User scrolling does not update this value.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">The assigned value is <see cref="float.NaN"/>.</exception>
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

        internal readonly struct ScrollToRatioMessage
        {
            public readonly float Ratio;

            public ScrollToRatioMessage(float ratio)
            {
                Ratio = ratio;
            }
        }
    }
}