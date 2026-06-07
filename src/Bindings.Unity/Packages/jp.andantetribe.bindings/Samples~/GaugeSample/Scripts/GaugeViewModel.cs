#nullable enable

using Binder;
using UnityEngine;

namespace Bindings.Sample
{
    [ViewModel]
    public partial class GaugeViewModel
    {
        [Required]
        private readonly float _maxRight;

        [SerializeField]
        [Required]
        [Schema(PathResolver.TMPro.TMP_Text.text, tooltip: "最大値")]
        private uint _max;

        [SerializeField]
        [Required]
        [Schema(PathResolver.TMPro.TMP_Text.text, tooltip: "現在値")]
        private uint _current;

        [SerializeField]
        [Schema(PathResolver.UnityEngine.RectTransform.offsetMax, tooltip: "真ゲージ")]
        private Vector2 _fillOffsetMax = Vector2.zero; // x : -right, y : -top で yは変えない.

        [SerializeField]
        [Schema(PathResolver.UnityEngine.RectTransform.offsetMax, tooltip: "エフェクト")]
        private Vector2 _effectOffsetMax = Vector2.zero; // x : -right, y : -top で yは変えない.

        public void Initialize(uint max, uint current)
        {
            _max = max;
            _current = current;
            _fillOffsetMax.x = _effectOffsetMax.x = _maxRight * (1 - (float)current / max);
            PublishRebindMessage();
        }

        public void SetValue(uint current)
        {
            if (_current == current)
            {
                // 同じ値なら何もしない.
                return;
            }

            var previous = _current;
            _current = current;

            var preFillOffsetMaxX = _fillOffsetMax.x;
            var increase = previous < current;  // e.g. heal...etc

            _fillOffsetMax.x = _maxRight * (1 - (float)current / _max);
            if (increase)
            {
                _effectOffsetMax = _fillOffsetMax;
            }
            _publisher.Publish(new GaugeValueChangedMessage(increase, preFillOffsetMaxX));
        }

        internal void UpdateEffectOffsetMaxX(float x) => _effectOffsetMax.x = x;
    }
}