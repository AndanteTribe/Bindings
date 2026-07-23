#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Bindings
{
    /// <summary>
    /// Represents a view that can be assigned a ViewModel and asked to update itself from that ViewModel.
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Updates the view from the ViewModel previously supplied to <see cref="Initialize"/>.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the binding operation.</param>
        /// <returns>A task-like value that completes when binding finishes.</returns>
        ValueTask BindAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether this view accepts the supplied ViewModel.
        /// </summary>
        /// <param name="viewModel">The ViewModel to check.</param>
        /// <returns><see langword="true"/> if the view accepts the ViewModel; otherwise, <see langword="false"/>.</returns>
        bool CanBind(IViewModel viewModel);

        /// <summary>
        /// Assigns a ViewModel to this view.
        /// </summary>
        /// <param name="viewModel">The ViewModel to assign.</param>
        void Initialize(IViewModel viewModel);
    }

    /// <summary>
    /// Represents a view that accepts ViewModels of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The ViewModel type accepted by the view.</typeparam>
    /// <remarks>
    /// This interface provides default implementations of <see cref="IView.CanBind"/> and
    /// <see cref="IView.Initialize"/> using runtime type checks against <typeparamref name="T"/>.
    /// </remarks>
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
        /// Assigns a strongly typed ViewModel to this view.
        /// </summary>
        /// <param name="viewModel">The ViewModel to assign.</param>
        void Initialize(T viewModel);
    }
}
