#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Bindings.Internal
{
    internal static class ValueTaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.DebuggerNonUserCode]
        public static void Forget(this ValueTask task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                CompleteSynchronously(awaiter);
            }
            else
            {
                RegisterContinuation(awaiter);
            }

            // Keep consumption protected even when a custom source reports success but throws in GetResult.
            static void CompleteSynchronously(ValueTaskAwaiter awaiter)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    if (Debug.isDebugBuild)
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            static void RegisterContinuation(ValueTaskAwaiter awaiter)
                => awaiter.UnsafeOnCompleted(State.Create(awaiter).Continuation);
        }

        private sealed class State
        {
            private static State? s_head;
            private State? _next;

            private ValueTaskAwaiter _awaiter;

            public readonly Action Continuation;

            private State() => Continuation = Complete;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static State Create(ValueTaskAwaiter awaiter)
            {
                if (s_head == null)
                {
                    return new State
                    {
                        _awaiter = awaiter
                    };
                }

                var instance = s_head;
                s_head = instance._next;
                instance._next = null;
                instance._awaiter = awaiter;
                return instance;
            }

            private void Complete()
            {
                try
                {
                    _awaiter.GetResult();
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    if (Debug.isDebugBuild)
                    {
                        Debug.LogException(ex);
                    }
                }
                finally
                {
                    // reset
                    _awaiter = default;
                    _next = s_head;
                    s_head = this;
                }
            }
        }
    }
}