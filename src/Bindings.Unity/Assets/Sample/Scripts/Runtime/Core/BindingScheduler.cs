#nullable enable

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
    }
}