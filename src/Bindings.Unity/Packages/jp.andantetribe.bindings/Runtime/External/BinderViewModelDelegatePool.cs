#if ENABLE_VCONTAINER
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VContainer;

namespace Bindings
{
    internal static class BinderViewModelDelegatePool
    {
        private static readonly ConcurrentBag<Entry> s_pool = new();

        public static Entry Rent(List<BinderViewModelRegistrationBuilder> registrations)
        {
            if (!s_pool.TryTake(out var entry))
            {
                entry = new Entry();
            }

            entry.Registrations = registrations;
            return entry;
        }

        internal sealed class Entry
        {
            public List<BinderViewModelRegistrationBuilder> Registrations = null!;

            public readonly Func<IObjectResolver, object> Resolve;

            public readonly Action<IObjectResolver> Return;

            public Entry()
            {
                Resolve = ResolveViewModels;
                Return = ReturnToPool;
            }

            private object ResolveViewModels(IObjectResolver resolver)
            {
                var viewModels = new IViewModel[Registrations.Count];
                for (var i = 0; i < Registrations.Count; i++)
                {
                    viewModels[i] = (IViewModel)resolver.Resolve(Registrations[i].Registration);
                }

                return viewModels;
            }

            private void ReturnToPool(IObjectResolver resolver)
            {
                Registrations = null!;
                s_pool.Add(this);
            }
        }
    }
}
#endif
