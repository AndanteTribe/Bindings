#nullable enable

using System.Threading;
using UnityEngine;

namespace Bindings.Sample
{
    [ViewModel]
    public partial class CountViewModel : ISubscriber
    {
        [Model]
        private readonly CountModel _model;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count += 1;
            _publisher.Publish<CountViewModel>();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            _publisher.Publish<CountViewModel>();
        }

        /// <inheritdoc />
        void ISubscriber.OnReceived()
        {
            _model.Count = _count;
        }
    }

    [System.Serializable]
    public partial class CountViewModel : IViewModel
    {
        private readonly IPublisher _publisher;

        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                _publisher.Publish<CountViewModel>();
            }
        }

        public CountViewModel(CountModel model, IPublisher publisher)
        {
            _model = model;
            _publisher = publisher;
        }

        private CountViewModel() : this(null!, null!)
        {
        }
    }

    [System.Serializable]
    public partial class CountView : IView<CountViewModel>
    {
        private CountViewModel _viewModel = null!;

        [SerializeField]
        private TMPro.TMP_Text _text = null!;

        [SerializeField]
        private UnityEngine.UI.Button _incrementButton = null!;

        [SerializeField]
        private UnityEngine.UI.Button _decrementButton = null!;

        public void Initialize(CountViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public System.Threading.Tasks.ValueTask BindAsync(CancellationToken _)
        {
            _text.text = _viewModel.Count.ToString();
            _incrementButton.onClick.RemoveAllListeners();
            _incrementButton.onClick.AddListener(_viewModel.Increment);
            _decrementButton.onClick.RemoveAllListeners();
            _decrementButton.onClick.AddListener(_viewModel.Decrement);

            return default;
        }
    }
}