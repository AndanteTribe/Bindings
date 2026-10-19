#nullable enable

using System.Collections.Generic;

namespace Bindings
{
    /// <summary>
    /// Routes an individual virtual scroll item's ViewModel messages to its currently assigned Binder.
    /// </summary>
    /// <remarks>Messages are ignored while no Binder is assigned to the item.</remarks>
    public sealed class ScrollItemMvvmPublisher : IMvvmPublisher
    {
        private readonly IReadOnlyList<IViewModel> _viewModels;
        private Binder? _binder;

        internal ScrollItemMvvmPublisher(IReadOnlyList<IViewModel> viewModels)
        {
            _viewModels = viewModels;
        }

        internal Binder? Binder
        {
            get => _binder == null ? _binder = null : _binder;
            set
            {
                if (value == null)
                {
                    _binder = null;
                    return;
                }
                _binder = value;
                _binder.Initialize(_viewModels);
                _binder.Run();
            }
        }

        /// <summary>
        /// Creates an item publisher and a container for registering the item's ViewModels.
        /// </summary>
        /// <param name="container">The container to populate with ViewModels constructed using the returned publisher.</param>
        /// <returns>The publisher shared by the item's ViewModels.</returns>
        public static ScrollItemMvvmPublisher Create(out ViewModelContainer container)
        {
            var viewModels = new List<IViewModel>();
            container = new ViewModelContainer(viewModels);
            return new ScrollItemMvvmPublisher(viewModels);
        }

        /// <inheritdoc />
        void IMvvmPublisher.PublishRebindMessage<T>()
        {
            if (Binder != null)
            {
                ((IMvvmPublisher)Binder).PublishRebindMessage<T>();
            }
        }

        /// <inheritdoc />
        void IMvvmPublisher.Publish<T>(T message)
        {
            if (Binder != null)
            {
                ((IMvvmPublisher)Binder).Publish(message);
            }
        }

        /// <summary>
        /// Registers the ViewModels assigned to a virtual scroll item's Binder when the item becomes visible.
        /// </summary>
        public readonly ref struct ViewModelContainer
        {
            private readonly List<IViewModel> _viewModels;

            internal ViewModelContainer(List<IViewModel> viewModels) => _viewModels = viewModels;

            /// <summary>
            /// Adds a ViewModel to this scroll item.
            /// </summary>
            /// <param name="viewModel">The ViewModel constructed using the item's publisher.</param>
            public void Regist(IViewModel viewModel) => _viewModels.Add(viewModel);
        }
    }
}