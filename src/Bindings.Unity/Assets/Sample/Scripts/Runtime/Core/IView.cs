#nullable enable

namespace Bindings
{
    /// <summary>
    /// A view that can be bound to a view model of type T.
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Binds the view to the view model.
        /// </summary>
        void Bind();

        /// <summary>
        /// Determines whether the view can be bound to the view model.
        /// </summary>
        /// <param name="viewModel">The view model to check.</param>
        /// <returns>true if the view can be bound to the view model; otherwise, false.</returns>
        bool CanBind(IViewModel viewModel);

        /// <summary>
        /// Initializes the view with the given view model.
        /// </summary>
        /// <param name="viewModel">The view model to bind to the view.</param>
        void Initialize(IViewModel  viewModel);
    }

    /// <summary>
    /// A view that can be bound to a view model of type T.
    /// </summary>
    /// <typeparam name="T">The type of the view model.</typeparam>
    public interface IView<in T> : IView where T : IViewModel
    {
        /// <inheritdoc />
        bool IView.CanBind(IViewModel viewModel) => viewModel is T;

        /// <inheritdoc />
        void IView.Initialize(IViewModel viewModel)
        {
            if (viewModel is T typedViewModel)
            {
                Initialize(typedViewModel);
                return;
            }
            throw new System.InvalidOperationException("Cannot initialize view with view model of type " + viewModel.GetType().FullName + ".");
        }

        /// <summary>
        /// Initializes the view with the given view model.
        /// </summary>
        /// <param name="viewModel">The view model to bind to the view.</param>
        void Initialize(T viewModel);
    }
}