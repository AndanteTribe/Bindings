#if ENABLE_VCONTAINER
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bindings.Internal;
using VContainer;

namespace Bindings
{
    internal static class ScrollViewModelDelegatePool<TItem>
    {
        private static readonly ConcurrentBag<Entry> s_pool = new();
        private static readonly ConcurrentBag<ItemEntry> s_itemPool = new();

        public static Entry Rent(
            Func<IObjectResolver, IReadOnlyList<TItem>> getItems,
            Action<BinderRegistration, TItem> configureItem)
        {
            if (!s_pool.TryTake(out var entry))
            {
                entry = new Entry();
            }

            entry.GetItems = getItems;
            entry.ConfigureItem = configureItem;
            return entry;
        }

        internal sealed class Entry
        {
            public Func<IObjectResolver, IReadOnlyList<TItem>> GetItems = null!;
            public Action<BinderRegistration, TItem> ConfigureItem = null!;

            public readonly Func<IObjectResolver, ReadOnlyMemory<ScrollItemMvvmPublisher>> Resolve;
            public readonly Action<IObjectResolver> Return;

            public Entry()
            {
                Resolve = ResolvePublishers;
                Return = ReturnToPool;
            }

            private ReadOnlyMemory<ScrollItemMvvmPublisher> ResolvePublishers(IObjectResolver resolver)
            {
                var items = GetItems(resolver);
                var publishers = new ScrollItemMvvmPublisher[items.Count];
                for (var i = 0; i < publishers.Length; i++)
                {
                    var publisher = new ScrollItemMvvmPublisher();
                    var entry = ItemEntry.Rent(publisher, items[i], ConfigureItem);
                    try
                    {
                        _ = resolver.CreateScope(entry.Configure);
                    }
                    finally
                    {
                        entry.Return();
                    }
                    publishers[i] = publisher;
                }
                return publishers;
            }

            private void ReturnToPool(IObjectResolver resolver)
            {
                GetItems = null!;
                ConfigureItem = null!;
                s_pool.Add(this);
            }
        }

        private sealed class ItemEntry
        {
            private ScrollItemMvvmPublisher _publisher = null!;
            private TItem _item = default!;
            private Action<BinderRegistration, TItem> _configureItem = null!;

            public readonly Action<IContainerBuilder> Configure;

            private ItemEntry() => Configure = ConfigureScope;

            public static ItemEntry Rent(
                ScrollItemMvvmPublisher publisher, TItem item, Action<BinderRegistration, TItem> configureItem)
            {
                if (!s_itemPool.TryTake(out var entry))
                {
                    entry = new ItemEntry();
                }

                entry._publisher = publisher;
                entry._item = item;
                entry._configureItem = configureItem;
                return entry;
            }

            private void ConfigureScope(IContainerBuilder builder)
            {
                builder.RegisterBuildCallback(static itemResolver =>
                {
                    var itemScope = (IScopedObjectResolver)itemResolver;
                    itemScope.Parent.Resolve<DisposableBag>().Add(itemScope);
                });
                using (var binding = new BinderRegistration(builder, _publisher, false))
                {
                    _configureItem(binding, _item);
                }
            }

            public void Return()
            {
                _publisher = null!;
                _item = default!;
                _configureItem = null!;
                s_itemPool.Add(this);
            }
        }
    }
}
#endif