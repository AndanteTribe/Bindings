#nullable enable

namespace Bindings
{
    public interface IMvvmPublisher
    {
        void PublishRebindMessage<T>() where T : IViewModel;

        void Publish<T>(T message);
    }
}