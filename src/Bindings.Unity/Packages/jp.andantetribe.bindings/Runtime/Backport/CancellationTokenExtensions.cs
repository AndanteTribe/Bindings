#if !NET6_0_OR_GREATER
#nullable enable

namespace System.Threading
{
    internal static class CancellationTokenExtensions
    {
        public static CancellationTokenRegistration UnsafeRegister(this CancellationToken cancellationToken, Action<object?> callback, object? state)
        {
            var restoreFlow = false;
            if (!ExecutionContext.IsFlowSuppressed())
            {
                _ = ExecutionContext.SuppressFlow();
                restoreFlow = true;
            }

            try
            {
                return cancellationToken.Register(callback, state, false);
            }
            finally
            {
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        }
    }
}

#endif