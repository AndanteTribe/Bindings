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
            else
            {
                awaiter.UnsafeOnCompleted(State.Create(awaiter).Continuation);
            }
        }

        private sealed class State
        {
            private static State? s_head;
            private State? _next;

            private ValueTaskAwaiter _awaiter;

            public readonly Action Continuation;

            private State() => Continuation = Complete;

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