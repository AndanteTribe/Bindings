#if ENABLE_VCONTAINER
#nullable enable

using VContainer;

namespace Bindings
{
    /// <summary>
    /// Provides VContainer registration helpers for Binders and ViewModels.
    /// </summary>
    public static class VContainerExtensions
    {
        /// <summary>
        /// Starts a deferred Binder registration. The using scope commits the registrations;
        /// the caller remains responsible for running the Binder.
        /// </summary>
        /// <param name="builder">The container builder that receives the registrations.</param>
        /// <param name="binder">The existing Binder component to register.</param>
        /// <param name="keyed">Whether to use the Binder instance as its registration key.</param>
        /// <returns>A registration scope that must end before the container is built.</returns>
        public static BinderRegistration RegisterBinder(this IContainerBuilder builder, Binder binder, bool keyed = false)
            => new(builder, binder, keyed);
    }
}

#endif