#if ENABLE_VCONTAINER
#nullable enable

using System.Runtime.CompilerServices;
using VContainer;

namespace Bindings
{
    public static class VContainerExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegistrationBuilder RegisterViewModel<T>(this IContainerBuilder builder, Binder binder, Lifetime lifetime = Lifetime.Scoped) where T : IViewModel
        {
            return builder.Register<T>(lifetime).As<IViewModel>().WithParameter(binder);
        }
    }
}

#endif