#nullable enable

namespace Bindings.Sample
{
    [ViewModel]
    public partial class CountViewModel
    {
        [Required]
        private readonly CountModel _model;

        [Schema(PathResolver.TMPro.TMP_Text.text)]
        public partial int Count { get; set; }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        private void Increment()
        {
            Count += 1;
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        private void Decrement()
        {
            Count -= 1;
        }

        partial void OnPostBind()
        {
            _model.Count = Count;
        }
    }

    // Planned to be generated auto.
    // public sealed partial class CountViewModel : IViewModel
    // {
    //     private readonly IMvvmPublisher _publisher;
    //     internal readonly global::UnityEngine.Events.UnityAction _incrementAction;
    //     internal readonly global::UnityEngine.Events.UnityAction _decrementAction;
    //
    //     public partial int Count
    //     {
    //         get => field;
    //         set
    //         {
    //             field = value;
    //             _publisher.PublishRebindMessage<CountViewModel>();
    //         }
    //     }
    //
    //     public CountViewModel(CountModel model, IMvvmPublisher publisher)
    //     {
    //         _publisher = publisher;
    //         _model = model;
    //         this._incrementAction = this.@Increment;
    //         this._decrementAction = this.@Decrement;
    //     }
    //
    //     public void NotifyCompletedBind() => OnPostBind();
    //
    //     partial void OnPostBind();
    // }

    // Planned to be generated auto.
    // [System.Serializable]
    // public sealed partial class CountView : IView<CountViewModel>
    // {
    //     [NonSerialized]
    //     private CountViewModel _viewModel = null!;
    //
    //     [SerializeField]
    //     private TMPro.TMP_Text _text = null!;
    //
    //     [SerializeField]
    //     private UnityEngine.UI.Button _incrementButton = null!;
    //
    //     [SerializeField]
    //     private UnityEngine.UI.Button _decrementButton = null!;
    //
    //     void IView<CountViewModel>.Initialize(CountViewModel viewModel)
    //     {
    //         _viewModel = viewModel;
    //     }
    //
    //     System.Threading.Tasks.ValueTask IView.BindAsync(CancellationToken _)
    //     {
    //         BindAll();
    //         return default;
    //     }
    //
    //     private void BindAll()
    //     {
    //         _text.SetValue(_viewModel.Count);
    //         _incrementButton.onClick.RemoveAllListeners();
    //         _incrementButton.onClick.AddListener(_viewModel._incrementAction);
    //         _decrementButton.onClick.RemoveAllListeners();
    //         _decrementButton.onClick.AddListener(_viewModel._decrementAction);
    //         OnPostBind();
    //         _viewModel.NotifyCompletedBind();
    //     }
    //
    //     partial void OnPostBind();
    // }

    // #if UNITY_EDITOR || !DISABLE_DEBUGTOOLKIT
    //     public sealed partial class CountView : IMvvmSubscriber<global::Bindings.DebugBindMessage>
    //     {
    //         void IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(global::Bindings.DebugBindMessage message)
    //         {
    //             message.BindTo(this);
    //             _text.SetValue(_viewModel.Count);
    //             OnPostBind();
    //             _viewModel.NotifyCompletedBind();
    //         }
    //     }
    // #endif
}
