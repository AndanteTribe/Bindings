#nullable enable

using System;
using System.Threading;

namespace Bindings
{
    internal sealed class StateTuple
    {
        private static StateTuple? s_head;
        private StateTuple? _next;

        private BindingValueSource? _source;
        private CancellationToken _cancellationToken;

        private StateTuple()
        {
        }

        public static StateTuple Create(BindingValueSource source, CancellationToken cancellationToken)
        {
            if (s_head == null)
            {
                return new StateTuple
                {
                    _source = source,
                    _cancellationToken = cancellationToken
                };
            }

            var instance = s_head;
            s_head = instance._next;
            instance._next = null;
            instance._source = source;
            instance._cancellationToken = cancellationToken;
            return instance;
        }

        public void Deconstruct(out BindingValueSource source, out CancellationToken cancellationToken)
        {
            source = _source ?? throw new InvalidOperationException("StateTuple is not initialized.");
            cancellationToken = _cancellationToken;

            // reset
            _source = null;
            _cancellationToken = CancellationToken.None;
            _next = s_head;
            s_head = this;
        }
    }
}