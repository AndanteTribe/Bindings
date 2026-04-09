#nullable enable

namespace Bindings
{
    public interface IMvvmSubscriber<in T> : IView
    {
        void OnReceivedMessage(T message);
    }
}