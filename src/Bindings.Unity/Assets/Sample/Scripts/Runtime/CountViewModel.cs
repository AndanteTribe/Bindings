#nullable enable

using System;

namespace Bindings.Sample;
public partial class CountViewModel : IViewModel
{
    private readonly CountModel _model;

    //public partial int Count { get; set; }

    public CountViewModel(CountModel model)
    {
        _model = model;
        int[] array = [1, 2];
    }

    public void A(params ReadOnlySpan<int> array)
    {

    }
}