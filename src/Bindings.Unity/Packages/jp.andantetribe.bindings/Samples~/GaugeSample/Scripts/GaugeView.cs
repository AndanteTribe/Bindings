#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Bindings;
using UnityEngine;
using LitMotion;

namespace Bindings.Sample
{
    public partial class GaugeView : IAsyncMvvmSubscriber<GaugeAnimationMessage>
    {
        [SerializeField]
        private RectTransform _gaugeRoot = null!;
        [SerializeField]
        private RectTransform _fillTransform = null!;
        [SerializeField]
        private RectTransform _effectTransform = null!;

        [SerializeField, Min(0.1f)]
        private float _gaugeAnimDuration = 1f;
        [SerializeField, Min(0.1f)]
        private float _decreaseAnimWaitDuration = 2f;

        private CancellationTokenSource? _valueChangeSource;
        private float _maxRight;

        partial void OnPostBind()
        {
            if (_maxRight == 0)
            {
                _maxRight = -_gaugeRoot.rect.width;
            }

            _fillTransform.offsetMax = CalculateOffsetMax();

            // エフェクトは使うときまで非表示.
            _effectTransform.gameObject.SetActive(false);
        }

        public async ValueTask OnReceivedMessageAsync(GaugeAnimationMessage message, CancellationToken cancellationToken)
        {
            // 前の演出が終わってないときはキャンセルで停止.
            _valueChangeSource?.Cancel();
            _valueChangeSource?.Dispose();

            // テキストの現在値は即更新.
            _currentText.SetValue(_viewModel.Current);

            _valueChangeSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationToken = _valueChangeSource.Token;

            // offsetMax.x ≒ -right なので _maxRight = -200 ならoffsetMax.xは -200 ~ 0 で推移.
            if (message.Previous < _viewModel.Current) // e.g. heal...etc
            {
                // エフェクトは使わないので非表示.
                _effectTransform.gameObject.SetActive(false);
                await LMotion.Create(_fillTransform.offsetMax, CalculateOffsetMax(), _gaugeAnimDuration)
                    .Bind(_fillTransform, static (value, gauge) => gauge.offsetMax = value)
                    .ToValueTask(CancelBehavior.Complete, cancellationToken);
            }
            else
            {
                // ゲージのほうは即更新.
                var previousFullOffsetMax = _fillTransform.offsetMax;
                var fullOffsetMax = _fillTransform.offsetMax = CalculateOffsetMax();

                // エフェクトは使うので初期化して表示.
                var effectObj = _effectTransform.gameObject;
                if (!effectObj.activeSelf)
                {
                    _effectTransform.offsetMax = previousFullOffsetMax;
                    effectObj.SetActive(true);
                }

                // 若干待機してからエフェクトを再生.
                await Awaitable.WaitForSecondsAsync(_decreaseAnimWaitDuration, cancellationToken);
                await LMotion.Create(_effectTransform.offsetMax, fullOffsetMax, _gaugeAnimDuration)
                    .Bind(_effectTransform, static (value, effect) => effect.offsetMax = value)
                    .ToValueTask(CancelBehavior.Cancel, cancellationToken);

                // エフェクトは非表示に戻す.
                effectObj.SetActive(false);
            }
        }

        private Vector2 CalculateOffsetMax()
        {
            var x = _maxRight * (1 - (float)_viewModel.Current / _viewModel.Max);
            // x : -right, y : -top で yは変えない.
            return new Vector2(x, 0);
        }
    }
}