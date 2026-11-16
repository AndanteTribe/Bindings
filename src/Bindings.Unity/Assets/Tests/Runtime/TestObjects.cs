#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Bindings.Tests
{
    internal static class TestObjects
    {
        public static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field!.SetValue(target, value);
        }

        public static Binder CreateBinder(params IView[] views)
        {
            var binder = new GameObject("Test Binder", typeof(RectTransform)).AddComponent<Binder>();
            SetField(binder, "_views", views);
            return binder;
        }
    }

    internal sealed class TestViewModel : IViewModel
    {
    }

    internal sealed class OtherViewModel : IViewModel
    {
    }

    [Serializable]
    internal sealed class RecordingView : IView<TestViewModel>, IMvvmSubscriber<int>
    {
        public TestViewModel? Model;
        public int BindCount;
        public int Message;
        public CancellationToken Token;

        public void Initialize(TestViewModel viewModel) => Model = viewModel;

        public ValueTask BindAsync(CancellationToken cancellationToken)
        {
            BindCount++;
            Token = cancellationToken;
            return default;
        }

        public void OnReceivedMessage(int message) => Message = message;
    }

    internal sealed class RecordingPublisher : IMvvmPublisher
    {
        public readonly List<object> Messages = new();
        public int RebindCount;

        public void PublishRebindMessage<T>() where T : IViewModel => RebindCount++;
        public void Publish<T>(T message) => Messages.Add(message!);
    }
}