#nullable enable

using System.Collections.Generic;

namespace Bindings
{
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

        public static ScrollItemMvvmPublisher Create(out ViewModelContainer container)
        {
            var viewModels = new List<IViewModel>();
            container = new ViewModelContainer(viewModels);
            return new ScrollItemMvvmPublisher(viewModels);
        }

        void IMvvmPublisher.PublishRebindMessage<T>()
        {
            if (Binder != null)
            {
                ((IMvvmPublisher)Binder).PublishRebindMessage<T>();
            }
        }

        void IMvvmPublisher.Publish<T>(T message)
        {
            if (Binder != null)
            {
                ((IMvvmPublisher)Binder).Publish(message);
            }
        }

        public readonly ref struct ViewModelContainer
        {
            private readonly List<IViewModel> _viewModels;

            internal ViewModelContainer(List<IViewModel> viewModels) => _viewModels = viewModels;

            public void Regist(IViewModel viewModel) => _viewModels.Add(viewModel);
        }
    }
}