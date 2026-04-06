// #nullable enable
//
// namespace Bindings.Sample
// {
//     [global::System.Serializable]
//     public partial class CountViewModel6 : IViewModel
//     {
//         private readonly global::Bindings.IMvvmPublisher _publisher;
//
//         public int Count
//         {
//             get => _count;
//             set
//             {
//                 _count = value;
//                 PublishRebindMessage();
//             }
//         }
//
//         public CountViewModel6(global::Bindings.Sample.CountModel model, global::Bindings.IMvvmPublisher publisher)
//         {
//             _model = model;
//             _publisher = publisher;
//         }
//
//         public void NotifyCompletedBind() => OnPostBind();
//
//         partial void OnPostBind();
//
//         [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
//         private void PublishRebindMessage()
//         {
//             _publisher.PublishRebindMessage<CountViewModel6>();
//         }
//     }
// }