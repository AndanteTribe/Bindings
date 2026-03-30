using System;

namespace Bindings
{
    public interface ISubscriber<in T>
    {
        void OnReceivedMessage(T message);
    }
}