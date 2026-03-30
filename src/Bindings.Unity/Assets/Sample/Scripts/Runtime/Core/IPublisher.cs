#nullable enable

namespace Bindings
{
    public interface IPublisher
    {
        void PublishRebindMessage<T>() where T : IViewModel;

        void Publish<T>(T message);
    }
}