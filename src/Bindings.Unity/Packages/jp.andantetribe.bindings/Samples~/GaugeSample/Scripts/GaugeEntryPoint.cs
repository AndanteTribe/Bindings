#nullable enable

using Bindings;
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
            _gaugeViewModel = new GaugeViewModel(_binder);
            _binder.Initialize(_gaugeViewModel);
            _gaugeViewModel.Max = _gaugeViewModel.Current = 1000;
        }

        private async void Start()
        {
            await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);
            _gaugeViewModel!.AnimateTo(500);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.AnimateTo(300);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.AnimateTo(450);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.AnimateTo(600);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.AnimateTo(500);
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
            _gaugeViewModel.AnimateTo(700);
            await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);
            _gaugeViewModel.AnimateTo(900);
            await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);
            _gaugeViewModel.AnimateTo(500);
        }
    }
}