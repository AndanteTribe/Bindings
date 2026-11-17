#nullable enable

using Bindings;
using UnityEngine;

namespace Bindings.Sample
{
    [ViewModel]
    public partial class GaugeViewModel
    {
        [Schema(PathResolver.TMPro.TMP_Text.text, tooltip: "最大値")]
        public partial uint Max { get; set; }

        [Schema(PathResolver.TMPro.TMP_Text.text, tooltip: "現在値")]
        public partial uint Current { get; set; }

        public void AnimateTo(uint current)
        {
            if (Current == current)
            {
                // 同じ値なら何もしない.
                return;
            }

            var previous = Current;
            Current = current;
            _publisher.Publish(new GaugeAnimationMessage(previous));
        }
    }
}
