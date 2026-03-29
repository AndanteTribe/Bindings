using Bindings;

namespace Bindings
{
    public interface IPublisher
    {
        void Publish<T>() where T : IViewModel;
    }
}