#if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT

#nullable enable

namespace Bindings
{
    public readonly struct DebugBindMessage
    {
        private readonly IViewModel _viewModel;

        public DebugBindMessage(IViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void BindTo<T>(IView<T> view) where T : IViewModel
        {
            if (_viewModel is T)
            {
                view.Initialize(_viewModel);
            }
        }
    }
}

#endif