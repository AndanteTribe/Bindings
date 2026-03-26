#nullable enable

namespace Bindings
{
    /// <summary>
    /// A view that can be bound to a view model of type T.
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Initializes the view with the given view model.
        /// </summary>
        /// <param name="viewModel">The view model to bind to the view.</param>
        /// <typeparam name="T">The type of the view model.</typeparam>
        void Initialize<T>(T  viewModel) where T : IViewModel;

        /// <summary>
        /// Binds the view to the view model.
        /// </summary>
        void Bind();
    }
}