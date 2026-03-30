#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Bindings
{
    public static class ValueTaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forget(this ValueTask task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogException(ex);
#endif
                }
            }
            else
            {
                awaiter.UnsafeOnCompleted(State.Create(awaiter).Dispose);
            }
        }

        private sealed class State : IDisposable
        {
            private static State? s_head;
            private State? _next;

            private ValueTaskAwaiter _awaiter;

            private State()
            {
            }

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

            public void Dispose()
            {
                try
                {
                    _awaiter.GetResult();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogException(ex);
#endif
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