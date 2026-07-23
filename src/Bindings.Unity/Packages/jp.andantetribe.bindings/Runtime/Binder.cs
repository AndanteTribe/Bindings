#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bindings.Internal;
using UnityEngine;

namespace Bindings
{
    /// <summary>
    /// Assigns compatible ViewModels to registered views and invokes their binding operations.
    /// </summary>
    /// <remarks>
    /// Register <see cref="IView"/> instances in the Inspector, call <see cref="Initialize(IViewModel)"/> for each
    /// ViewModel, and then call <see cref="Run"/>. This component also implements <see cref="IMvvmPublisher"/> to
    /// route rebind requests and messages to its registered views.
    /// </remarks>
    [DefaultExecutionOrder(-1000)]
    public sealed class Binder : MonoBehaviour, IMvvmPublisher
    {
        [SerializeField]
        private bool _runOnStart = false;

        [SerializeReference]
        private IView[] _views = Array.Empty<IView>();

        private readonly List<IView> _nextChangedViews = new();
        private CancellationTokenSource _cancellationTokenSource = null!;

        private void Awake()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        }

        private void Start()
        {
            if (_runOnStart)
            {
                Run();
            }
        }

        /// <summary>
        /// Assigns each supplied ViewModel to every registered view that accepts it.
        /// </summary>
        /// <param name="viewModels">The ViewModels to assign to compatible views.</param>
        /// <exception cref="InvalidOperationException">
        /// No registered view accepts one of the supplied ViewModels.
        /// </exception>
#if ENABLE_VCONTAINER
        [VContainer.Inject]
#endif
        public void Initialize(IReadOnlyList<IViewModel> viewModels)
        {
            for (var i = 0; i < viewModels.Count; i++)
            {
                Initialize(viewModels[i]);
            }
        }

        /// <summary>
        /// Assigns a ViewModel to every registered view that accepts it.
        /// </summary>
        /// <param name="viewModel">The ViewModel to assign to compatible views.</param>
        /// <exception cref="InvalidOperationException">No registered view accepts <paramref name="viewModel"/>.</exception>
        public void Initialize(IViewModel viewModel)
        {
            var found = false;
            foreach (var view in _views.AsSpan())
            {
                if (view.CanBind(viewModel))
                {
                    view.Initialize(viewModel);
                    found = true;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("No view found for view model of type " + viewModel.GetType().FullName + ".");
            }
        }

        /// <summary>
        /// Requests cancellation of pending or active binding operations, then invokes <see cref="IView.BindAsync"/>
        /// on every registered view with a new cancellation token.
        /// </summary>
        /// <remarks>
        /// This method returns after invoking each view; it does not wait for binding operations to complete.
        /// </remarks>
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
        void IMvvmPublisher.PublishRebindMessage<T>()
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
                    RunAsync(view, _nextChangedViews, _cancellationTokenSource.Token).Forget();
                    return;
                }
            }
            throw new InvalidOperationException("No view found for view model of type " + typeof(T).FullName + ".");
        }

        /// <inheritdoc />
        void IMvvmPublisher.Publish<T>(T message)
        {
            foreach (var view in _views.AsSpan())
            {
                switch (view)
                {
                    case IMvvmSubscriber<T> subscriber:
                        subscriber.OnReceivedMessage(message);
                        break;
                    case IAsyncMvvmSubscriber<T> subscriber:
                        subscriber.OnReceivedMessageAsync(message, destroyCancellationToken).Forget();
                        break;
                }
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        private static async ValueTask RunAsync(IView view, IList<IView> nextChangedViews, CancellationToken cancellationToken)
        {
            try
            {
                await BindingScheduler.EnqueueAsync(cancellationToken);
            }
            finally
            {
                nextChangedViews.Remove(view);
            }
            await view.BindAsync(cancellationToken);
        }
    }
}
