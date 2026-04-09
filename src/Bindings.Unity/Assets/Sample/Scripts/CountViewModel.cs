#nullable enable

using System;
using System.Threading;
using UnityEngine;

namespace Bindings.Sample
{
    [ViewModel]
    public partial class CountViewModel
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
            _publisher.PublishRebindMessage<CountViewModel>();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            _publisher.PublishRebindMessage<CountViewModel>();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
        }
    }

    // Planned to be generated auto.
    // [System.Serializable]
    // public sealed partial class CountViewModel : IViewModel
    // {
    //     private readonly IMvvmPublisher _publisher;
    //
    //     public int Count
    //     {
    //         get => _count;
    //         set
    //         {
    //             _count = value;
    //             _publisher.PublishRebindMessage<CountViewModel>();
    //         }
    //     }
    //
    //     public CountViewModel(CountModel model, IMvvmPublisher publisher)
    //     {
    //         _model = model;
    //         _publisher = publisher;
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
    //         _incrementButton.onClick.AddListener(_viewModel.Increment);
    //         _decrementButton.onClick.RemoveAllListeners();
    //         _decrementButton.onClick.AddListener(_viewModel.Decrement);
    //         OnPostBind();
    //         _viewModel.NotifyCompletedBind();
    //     }
    //
    //     partial void OnPostBind();
    // }

// #if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT
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