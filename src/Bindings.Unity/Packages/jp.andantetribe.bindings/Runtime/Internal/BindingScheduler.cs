#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Bindings.Internal
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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    Canvas.preWillRenderCanvases -= Run;
                }
            }
#endif
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
            var source = BindingValueSource.Create(cancellationToken);
            using var _ = cancellationToken.UnsafeRegister(static s =>
            {
                var source = (BindingValueSource)s!;
                source.SetCancel();
                s_queue.Remove(source);
            }, source);

            s_queue.Add(source);
            await new ValueTask(source, source.Version);
        }
    }
}