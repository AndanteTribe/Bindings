#nullable enable

using UnityEngine;

namespace Bindings.Sample
{
    /// <summary>
    /// Populates the two virtual grids in the VirtualScrollSample scene.
    /// </summary>
    public sealed class VirtualScrollSample : MonoBehaviour
    {
        [SerializeField]
        private Binder _verticalBinder = null!;
        [SerializeField]
        private Binder _horizontalBinder = null!;

        private SingleAxisVirtualScrollViewModel? _verticalViewModel;
        private SingleAxisVirtualScrollViewModel? _horizontalViewModel;

        private void Start()
        {
            _verticalViewModel = Populate(_verticalBinder);
            _horizontalViewModel = Populate(_horizontalBinder);
        }

        public void ScrollVertical(float ratio)
        {
            if (_verticalViewModel != null)
            {
                _verticalViewModel.ScrollRatio = ratio;
            }
        }

        public void ScrollHorizontal(float ratio)
        {
            if (_horizontalViewModel != null)
            {
                _horizontalViewModel.ScrollRatio = ratio;
            }
        }

        private static SingleAxisVirtualScrollViewModel Populate(Binder binder)
        {
            var publishers = new ScrollItemMvvmPublisher[1000];
            for (var i = 0; i < publishers.Length; i++)
            {
                publishers[i] = ScrollItemMvvmPublisher.Create(out var container);
                container.Register(new VirtualScrollSampleItemViewModel($"Item {i:0000}", publishers[i]));
            }

            var viewModel = new SingleAxisVirtualScrollViewModel(publishers, binder);
            binder.Initialize(viewModel);
            binder.Run();
            return viewModel;
        }
    }

    [ViewModel]
    public sealed partial class VirtualScrollSampleItemViewModel
    {
        [Required]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private string _label;
    }
}
