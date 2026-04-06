// #nullable enable
//
// namespace Bindings.Sample
// {
//     [global::System.Serializable]
//     public sealed partial class CountView4 : IView<global::Bindings.Sample.CountViewModel4>
//     {
//         [global::System.NonSerialized]
//         private global::Bindings.Sample.CountViewModel4 _viewModel = null!;
//
//         [global::UnityEngine.SerializeField]
//         private global::TMPro.TMP_Text _text = null!;
//
//         [global::UnityEngine.SerializeField]
//         private global::UnityEngine.UI.Button _button1 = null!;
//
//         [global::UnityEngine.SerializeField]
//         private global::UnityEngine.UI.Button _button2 = null!;
//
//         void global::Bindings.IView<global::Bindings.Sample.CountViewModel4>.Initialize(global::Bindings.Sample.CountViewModel4 viewModel)
//         {
//             _viewModel = viewModel;
//         }
//
//         global::System.Threading.Tasks.ValueTask global::Bindings.IView.BindAsync(global::System.Threading.CancellationToken _)
//         {
//             BindAll();
//             return default;
//         }
//
//         private void BindAll()
//         {
//             global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count);
//             _button1.onClick.RemoveAllListeners();
//             _button1.onClick.AddListener(_viewModel.Increment);
//             _button2.onClick.RemoveAllListeners();
//             _button2.onClick.AddListener(_viewModel.Decrement);
//             OnPostBind();
//             _viewModel.NotifyCompletedBind();
//         }
//
//         partial void OnPostBind();
//     }
//
// #if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT
//     public sealed partial class CountView4 : IMvvmSubscriber<global::Bindings.DebugBindMessage>
//     {
//         void IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(global::Bindings.DebugBindMessage message)
//         {
//             message.BindTo(this);
//             global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count);
//             OnPostBind();
//             _viewModel.NotifyCompletedBind();
//         }
//     }
// #endif
// }