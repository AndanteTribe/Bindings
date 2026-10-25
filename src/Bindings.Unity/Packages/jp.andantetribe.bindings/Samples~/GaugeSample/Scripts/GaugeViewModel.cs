#nullable enable

using Bindings;
using UnityEngine;

namespace Bindings.Sample
{
    [ViewModel]
    public partial class GaugeViewModel
    {
        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text, tooltip: "最大値")]
        private uint _max;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text, tooltip: "現在値")]
        private uint _current;

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
