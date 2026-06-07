#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Bindings;
using UnityEngine;
using LitMotion;

namespace Bindings.Sample
{
    public partial class GaugeView : IAsyncMvvmSubscriber<GaugeValueChangedMessage>
    {
        private CancellationTokenSource? _valueChangeSource;

        [SerializeField, Min(0.1f)]
        private float _gaugeAnimDuration = 1f;
        [SerializeField, Min(0.1f)]
        private float _decreaseAnimWaitDuration = 2f;

        partial void OnPostBind()
        {
            // エフェクトは使うときまで非表示.
            _rectTransform2.gameObject.SetActive(false);
        }

        public async ValueTask OnReceivedMessageAsync(GaugeValueChangedMessage message, CancellationToken cancellationToken)
        {
            // 前の演出が終わってないときはキャンセルで停止.
            _valueChangeSource?.Cancel();
            _valueChangeSource?.Dispose();

            // テキストの現在値は即更新.
            _text2.SetValue(_viewModel.Current);

            _valueChangeSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationToken = _valueChangeSource.Token;

            // offsetMax.x ≒ -right なので _maxRight = -200 ならoffsetMax.xは -200 ~ 0 で推移.
            if (message.Increase)
            {
                // エフェクトは使わないので非表示.
                _rectTransform2.gameObject.SetActive(false);
                await LMotion.Create(_rectTransform1.offsetMax.x, _viewModel.FillOffsetMax.x, _gaugeAnimDuration)
                    .Bind(_rectTransform1, static (value, gauge) => gauge.offsetMax = new Vector2(value, 0))
                    .ToValueTask(CancelBehavior.Complete, cancellationToken);
            }
            else
            {
                // ゲージのほうは即更新.
                _rectTransform1.offsetMax = new Vector2(_viewModel.FillOffsetMax.x, 0);
                // エフェクトは使うので初期化して表示.
                var effectObj = _rectTransform2.gameObject;
                if (!effectObj.activeSelf)
                {
                    _rectTransform2.offsetMax = new Vector2(message.PreFillOffsetMaxX, 0);
                    effectObj.SetActive(true);
                }
                // 若干待つ.
                await Awaitable.WaitForSecondsAsync(_decreaseAnimWaitDuration, cancellationToken);
                await LMotion.Create(_rectTransform2.offsetMax.x, _viewModel.FillOffsetMax.x, _gaugeAnimDuration)
                    .Bind(_viewModel, _rectTransform2, static (value, vm, effect) =>
                    {
                        vm.UpdateEffectOffsetMaxX(value);
                        effect.offsetMax = new Vector2(value, 0);
                    })
                    .ToValueTask(CancelBehavior.Cancel, cancellationToken);
                // エフェクトは非表示に戻す.
                effectObj.SetActive(false);
            }
        }
    }
}