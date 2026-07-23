#if ENABLE_VCONTAINER
#nullable enable

using System.Runtime.CompilerServices;
using VContainer;

namespace Bindings
{
    /// <summary>
    /// Provides VContainer registration helpers for ViewModels.
    /// </summary>
    public static class VContainerExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="T"/> as <see cref="IViewModel"/> and adds the supplied
        /// <see cref="Binder"/> as an explicit constructor parameter.
        /// </summary>
        /// <typeparam name="T">The ViewModel implementation to register.</typeparam>
        /// <param name="builder">The VContainer builder to add the registration to.</param>
        /// <param name="binder">The Binder supplied when constructing <typeparamref name="T"/>.</param>
        /// <param name="lifetime">The lifetime of the ViewModel registration.</param>
        /// <returns>The created VContainer registration builder.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegistrationBuilder RegisterViewModel<T>(this IContainerBuilder builder, Binder binder, Lifetime lifetime = Lifetime.Scoped) where T : IViewModel
        {
            return builder.Register<T>(lifetime).As<IViewModel>().WithParameter(binder);
        }
    }
}

#endif
