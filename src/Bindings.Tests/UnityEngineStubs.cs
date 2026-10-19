using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace UnityEngine;

public static class Debug
{
    private static readonly ConcurrentQueue<Exception> s_loggedExceptions = new();

    public static bool isDebugBuild { get; set; } = true;

    public static IReadOnlyCollection<Exception> LoggedExceptions => s_loggedExceptions.ToArray();

    public static void LogException(Exception exception)
    {
        s_loggedExceptions.Enqueue(exception);
    }

    public static void Reset()
    {
        isDebugBuild = true;
        while (s_loggedExceptions.TryDequeue(out _))
        {
        }
    }
}
