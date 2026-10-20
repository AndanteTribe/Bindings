#if ENABLE_VCONTAINER
#nullable enable

using System;
using System.Reflection;
using VContainer;
using VContainer.Internal;

namespace Bindings
{
    internal static class ViewModelInjectorCache<T> where T : IViewModel
    {
        public static readonly IInjector Injector;

        static ViewModelInjectorCache()
        {
            var injectorType = typeof(T).GetNestedType("__BindingsInjector", BindingFlags.NonPublic);
            Injector = injectorType != null && typeof(IInjector).IsAssignableFrom(injectorType)
                ? (IInjector)Activator.CreateInstance(injectorType, true)!
                : InjectorCache.GetOrBuild(typeof(T));
        }
    }
}

#endif