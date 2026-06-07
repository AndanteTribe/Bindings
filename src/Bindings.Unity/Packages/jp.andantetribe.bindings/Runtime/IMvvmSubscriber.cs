#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Bindings
{
    public interface IMvvmSubscriber<in T> : IView
    {
        void OnReceivedMessage(T message);
    }

    public interface IAsyncMvvmSubscriber<in T> : IView
    {
        ValueTask OnReceivedMessageAsync(T message, CancellationToken cancellationToken);
    }
}