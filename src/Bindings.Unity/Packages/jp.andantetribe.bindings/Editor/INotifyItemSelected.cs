#nullable enable

using System;

namespace Bindings.Editor
{
    public interface INotifyItemSelected
    {
        void ItemSelected(Type type);
    }
}