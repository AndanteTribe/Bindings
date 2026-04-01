#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Bindings
{
    /// <summary>
    /// The binding scheduler that runs before rendering the canvas.
    /// </summary>
    internal static class BindingScheduler
    {
        private static readonly List<BindingValueSource> s_queue = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Canvas.preWillRenderCanvases -= Run;
            Canvas.preWillRenderCanvases += Run;
        }

        private static void Run()
        {
            foreach (var queue in s_queue)
            {
                queue.SetResult();
            }
            s_queue.Clear();
        }

        public static async ValueTask EnqueueAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = BindingValueSource.Create();
            using var _ = cancellationToken.UnsafeRegister(tuple =>
            {
                var (s, ct) = (StateTuple)tuple!;
                s.SetCancel(ct);
                s_queue.Remove(s);
            }, StateTuple.Create(source, cancellationToken));

            s_queue.Add(source);
            await new ValueTask(source, source.Version);
        }

        private sealed class StateTuple
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
}