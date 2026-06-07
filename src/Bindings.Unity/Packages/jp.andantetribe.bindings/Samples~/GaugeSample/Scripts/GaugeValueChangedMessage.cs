#nullable enable

using UnityEngine;

namespace Bindings.Sample
{
    public readonly struct GaugeValueChangedMessage
    {
        public readonly bool Increase;
        public readonly float PreFillOffsetMaxX;

        public GaugeValueChangedMessage(bool increase, float preFillOffsetMaxX)
        {
            Increase = increase;
            PreFillOffsetMaxX = preFillOffsetMaxX;
        }
    }
}