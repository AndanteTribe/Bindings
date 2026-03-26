#nullable enable

using UnityEngine;

namespace Bindings
{
    public sealed class Binder : MonoBehaviour
    {
        [SerializeReference]
        private IView _view = null!;

        /// <summary>
        /// Initializes the view with the given view model.
        /// </summary>
        /// <param name="viewModel">The view model to bind to the view.</param>
        /// <typeparam name="T">The type of the view model.</typeparam>
        public void Initialize<T>(T viewModel) where T : IViewModel
        {
            _view.Initialize(viewModel);
        }

        /// <summary>
        /// Binds the view to the view model.
        /// </summary>
        public void Run()
        {
            _view.Bind();
        }
    }
}