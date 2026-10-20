#if ENABLE_VCONTAINER
#nullable enable

using System;
using System.Collections.Generic;
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
        private readonly Binder _binder;
        private readonly bool _keyed;
        private readonly List<BinderViewModelRegistrationBuilder> _viewModels;

        internal BinderRegistration(IContainerBuilder builder, Binder binder, bool keyed)
        {
            _builder = builder;
            _binder = binder;
            _keyed = keyed;
            _viewModels = new List<BinderViewModelRegistrationBuilder>();
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
            var viewModel = new BinderViewModelRegistrationBuilder(typeof(T), lifetime, ViewModelInjectorCache<T>.Injector);
            _viewModels.Add(viewModel);
            return viewModel.WithParameter<IMvvmPublisher>(_binder);
        }

        void IDisposable.Dispose()
        {
            var registrations = _viewModels;
            foreach (var registration in registrations)
            {
                _ = _builder.Register(registration);
            }

            var delegates = BinderViewModelDelegatePool.Rent(registrations);
            var binderRegistration = _builder.RegisterComponent(_binder)
                .WithParameter(typeof(IReadOnlyList<IViewModel>), delegates.Resolve);
            if (_keyed)
            {
                _ = binderRegistration.Keyed(_binder);
            }

            _builder.RegisterDisposeCallback(delegates.Return);
        }
    }
}
#endif
