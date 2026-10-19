#nullable enable

namespace Bindings
{
    public readonly struct ScrollToRatioMessage
    {
        public readonly float Ratio;

        public ScrollToRatioMessage(float ratio)
        {
            Ratio = ratio;
        }
    }
}