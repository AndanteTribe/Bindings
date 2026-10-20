#if ENABLE_VCONTAINER
#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VContainer;

namespace Bindings
{
    internal sealed class BindingsViewModelInstanceProvider : IInstanceProvider
    {
        private readonly IInjector _injector;
        private readonly IReadOnlyList<IInjectParameter> _parameters;

        public BindingsViewModelInstanceProvider(IInjector injector, IReadOnlyList<IInjectParameter> parameters)
        {
            _injector = injector;
            _parameters = parameters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object SpawnInstance(IObjectResolver resolver) => _injector.CreateInstance(resolver, _parameters);
    }
}

#endif