#nullable enable

namespace Bindings
{
    /// <summary>
    /// Defines operations for requesting binding refreshes and publishing messages.
    /// </summary>
    public interface IMvvmPublisher
    {
        /// <summary>
        /// Requests that bindings associated with ViewModel type <typeparamref name="T"/> be refreshed.
        /// </summary>
        /// <typeparam name="T">The ViewModel type whose bindings must be refreshed.</typeparam>
        void PublishRebindMessage<T>() where T : IViewModel;

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message to deliver.</param>
        void Publish<T>(T message);
    }
}
