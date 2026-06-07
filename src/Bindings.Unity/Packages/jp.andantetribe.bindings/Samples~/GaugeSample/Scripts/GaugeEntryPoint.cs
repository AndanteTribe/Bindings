#nullable enable

using Binder;
using UnityEngine;

namespace Bindings.Sample
{
    public class GaugeEntryPoint : MonoBehaviour
    {
        [SerializeField]
        private Binder _binder = null!;

        private GaugeViewModel? _gaugeViewModel;

        private void Awake()
        {
            var maxRight = ((RectTransform)_binder.transform).rect.width * -1;
            _gaugeViewModel = new GaugeViewModel(maxRight, 1000, 1000, _binder);
            _binder.Initialize(_gaugeViewModel);
        }

        private async void Start()
        {
            await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);
            _gaugeViewModel!.SetValue(500);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.SetValue(300);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.SetValue(450);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.SetValue(600);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.SetValue(500);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.SetValue(700);
            await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);
            _gaugeViewModel.SetValue(900);
            await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);
            _gaugeViewModel.SetValue(500);
        }
    }
}