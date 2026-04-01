#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        private CancellationTokenSource _cancellationTokenSource = null!;

        [Inject]
        public void Initialize(IReadOnlyList<IViewModel> viewModels)
        {
            foreach (var viewModel in viewModels)
            {
                Initialize(viewModel);
            }
        }

        private void Awake()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        }

        private void OnEnable()
        {
            if (_runOnEnable)
            {
                Run();
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
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            foreach (var view in _views.AsSpan())
            {
                view.BindAsync(_cancellationTokenSource.Token).Forget();
            }
        }

        /// <inheritdoc />
        void IPublisher.PublishRebindMessage<T>()
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
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                    }

                    _nextChangedViews.Add(view);
                    RunAsync(view, _cancellationTokenSource.Token).Forget();
                    return;
                }
            }
            throw new InvalidOperationException("No view found for view model of type " + typeof(T).FullName + ".");
        }

        void IPublisher.Publish<T>(T message)
        {
            foreach (var view in _views.AsSpan())
            {
                if (view is ISubscriber<T> subscriber)
                {
                    subscriber.OnReceivedMessage(message);
                }
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        private static async ValueTask RunAsync(IView view, CancellationToken cancellationToken)
        {
            await BindingScheduler.EnqueueAsync(cancellationToken);
            await view.BindAsync(cancellationToken);
        }
    }
}