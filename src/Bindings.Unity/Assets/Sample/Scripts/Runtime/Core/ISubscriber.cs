#nullable enable

namespace Bindings
{
    public interface ISubscriber<in T> : IView
    {
        void OnReceivedMessage(T message);
    }
}