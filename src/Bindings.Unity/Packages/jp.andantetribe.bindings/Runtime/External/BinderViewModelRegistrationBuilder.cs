#if ENABLE_VCONTAINER
#nullable enable

using System;
using VContainer;

namespace Bindings
{
    internal sealed class BinderViewModelRegistrationBuilder : RegistrationBuilder
    {
        private readonly IInjector _injector;

        public Registration Registration { get; private set; } = null!;

        public BinderViewModelRegistrationBuilder(Type implementationType, Lifetime lifetime, IInjector injector)
            : base(implementationType, lifetime)
        {
            _injector = injector;
        }

        public override Registration Build()
        {
            var instanceProvider = new BindingsViewModelInstanceProvider(_injector, Parameters);
            Registration = new Registration(ImplementationType, Lifetime, InterfaceTypes, instanceProvider, Key);
            return Registration;
        }
    }
}

#endif