#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Bindings
{
    public sealed class Binder : MonoBehaviour, IPublisher
    {
        [SerializeReference]
        private IView[] _views = null!;

        [SerializeField]
        private bool _runOnEnable = true;

        private readonly List<IView> _nextChangedViews = new();

        [Inject]
        public void Initialize(IReadOnlyList<IViewModel> viewModels)
        {
            foreach (var viewModel in viewModels)
            {
                Initialize(viewModel);
            }
        }

        private void OnEnable()
        {
            if (_runOnEnable)
            {
                Run();
            }
        }

        private void LateUpdate()
        {
            if (_nextChangedViews.Count > 0)
            {
                foreach (var view in _nextChangedViews)
                {
                    view.Bind();
                }
                _nextChangedViews.Clear();
            }
        }

        /// <summary>
        /// Initializes the view with the given view model.
        /// </summary>
        /// <param name="viewModel">The view model to bind to the view.</param>
        public void Initialize(IViewModel viewModel)
        {
            foreach (var view in _views.AsSpan())
            {
                if (view.CanBind(viewModel))
                {
                    view.Initialize(viewModel);
                    break;
                }
            }
            throw new InvalidOperationException("No view found for view model of type " + viewModel.GetType().FullName + ".");
        }

        /// <summary>
        /// Binds the view to the view model.
        /// </summary>
        public void Run()
        {
            foreach (var view in _views.AsSpan())
            {
                view.Bind();
            }
        }

        /// <inheritdoc />
        void IPublisher.Publish<T>()
        {
            foreach (var view in _nextChangedViews)
            {
                if (view is IView<T>)
                {
                    return;
                }
            }
            foreach (var view in _views.AsSpan())
            {
                if (view is IView<T>)
                {
                    _nextChangedViews.Add(view);
                }
            }
            throw new InvalidOperationException("No view found for view model of type " + typeof(T).FullName + ".");
        }
    }
}