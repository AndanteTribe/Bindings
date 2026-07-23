#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Bindings
{
    /// <summary>
    /// Defines a view that handles messages of type <typeparamref name="T"/> synchronously.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public interface IMvvmSubscriber<in T> : IView
    {
        /// <summary>
        /// Handles a published message.
        /// </summary>
        /// <param name="message">The published message.</param>
        void OnReceivedMessage(T message);
    }

    /// <summary>
    /// Defines a view that handles messages of type <typeparamref name="T"/> asynchronously.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public interface IAsyncMvvmSubscriber<in T> : IView
    {
        /// <summary>
        /// Handles a published message asynchronously.
        /// </summary>
        /// <param name="message">The published message.</param>
        /// <param name="cancellationToken">A token used to cancel message handling.</param>
        /// <returns>A task-like value that completes when the message has been handled.</returns>
        ValueTask OnReceivedMessageAsync(T message, CancellationToken cancellationToken);
    }
}
