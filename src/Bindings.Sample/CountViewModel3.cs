// #nullable enable
//
// namespace Bindings.Sample
// {
//     public partial class CountViewModel3 : IViewModel
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
//         public CountViewModel3(global::Bindings.Sample.CountModel model, global::Bindings.IMvvmPublisher publisher)
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
//             _publisher.PublishRebindMessage<CountViewModel3>();
//         }
//     }
// }