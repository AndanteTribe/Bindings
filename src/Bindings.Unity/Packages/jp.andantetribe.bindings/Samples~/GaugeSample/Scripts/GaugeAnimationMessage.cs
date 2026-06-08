#nullable enable

namespace Bindings.Sample
{
    public readonly struct GaugeAnimationMessage
    {
        public readonly uint Previous;

        public GaugeAnimationMessage(uint previous)
        {
            Previous = previous;
        }
    }
}