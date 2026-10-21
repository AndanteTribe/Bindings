#if ENABLE_VCONTAINER
#nullable enable

using System;
using System.Collections.Generic;
using Bindings.Internal;
using VContainer;
using VContainer.Unity;

namespace Bindings
{
    /// <summary>Collects ViewModels and registers them with an existing Binder when the using scope ends.</summary>
    /// <remarks>
    /// Declare this scope directly in a using statement or declaration. Do not copy, box, or dispose it twice.
    /// End the scope before building the container. The scope does not own or run the Binder.
    /// </remarks>
    public readonly struct BinderRegistration : IDisposable
    {
        private readonly IContainerBuilder _builder;
        private readonly IMvvmPublisher _publisher;
        private readonly bool _keyed;
        private readonly List<ViewModelRegistrationBuilder> _viewModels;

        internal BinderRegistration(IContainerBuilder builder, IMvvmPublisher publisher, bool keyed)
        {
            _builder = builder;
            _publisher = publisher;
            _keyed = keyed;
            _viewModels = new List<ViewModelRegistrationBuilder>();
        }

        /// <summary>Adds a deferred ViewModel registration and returns its native VContainer options builder.</summary>
        /// <typeparam name="T">The ViewModel implementation to construct.</typeparam>
        /// <param name="lifetime">The ViewModel lifetime.</param>
        /// <returns>A builder supporting AsSelf, Keyed, WithParameter, and other VContainer options.</returns>
        /// <remarks>
        /// The Binder is supplied as IMvvmPublisher. No service interfaces are added automatically;
        /// configure them explicitly when needed. This method does not register anything in the container yet.
        /// </remarks>
        public RegistrationBuilder WithViewModel<T>(Lifetime lifetime = Lifetime.Scoped) where T : IViewModel
        {
            var viewModel = new ViewModelRegistrationBuilder(typeof(T), lifetime, ViewModelInjectorCache<T>.Injector);
            _viewModels.Add(viewModel);
            return viewModel.WithParameter(_publisher);
        }

        public RegistrationBuilder WithScrollViewModel<TItem>(
            Func<IObjectResolver, IReadOnlyList<TItem>> getItems,
            Action<BinderRegistration, TItem> configureItem)
        {
            if (!_builder.Exists(typeof(DisposableBag)))
            {
                _ = _builder.Register<DisposableBag>(Lifetime.Scoped);
            }

            var entry = ScrollViewModelDelegatePool<TItem>.Rent(getItems, configureItem);
            _builder.RegisterDisposeCallback(entry.Return);
            return WithViewModel<SingleAxisVirtualScrollViewModel>()
                .WithParameter(entry.Resolve);
        }

        void IDisposable.Dispose()
        {
            var registrations = _viewModels;
            foreach (var registration in registrations)
            {
                _ = _builder.Register(registration);
            }

            var entry = ViewModelDelegatePool.Rent(registrations);
            RegistrationBuilder publisherRegistration;

            switch (_publisher)
            {
                case Binder binder:
                    publisherRegistration = _builder.RegisterComponent(binder)
                        .WithParameter(typeof(IReadOnlyList<IViewModel>), entry.Resolve);
                    break;
                case ScrollItemMvvmPublisher scrollItemPublisher:
                    publisherRegistration = _builder.RegisterInstance(scrollItemPublisher);
                    _ = _builder.RegisterInstance(entry);
                    _builder.RegisterBuildCallback(static resolver =>
                    {
                        var publisher = resolver.Resolve<ScrollItemMvvmPublisher>();
                        var entry = resolver.Resolve<ViewModelDelegatePool.Entry>();
                        publisher._viewModels.AddRange((IReadOnlyList<IViewModel>)entry.Resolve(resolver));
                    });
                    break;
                default:
                    throw new NotSupportedException("Only Binder and ScrollItemMvvmPublisher are supported.");
            }

            if (_keyed)
            {
                _ = publisherRegistration.Keyed(_publisher);
            }

            _builder.RegisterDisposeCallback(entry.Return);
        }
    }
}
#endif