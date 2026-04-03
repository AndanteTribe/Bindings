#nullable enable

using System;
using UnityEditor.IMGUI.Controls;

namespace Bindings.Editor
{
    public class AdvancedViewDropdown : AdvancedDropdown
    {
        private readonly INotifyItemSelected _inis;

        private readonly Type[] _viewTypes = TypeUtils.GetAllViewTypes();

        public AdvancedViewDropdown(INotifyItemSelected inis) : base(new AdvancedDropdownState())
        {
            _inis = inis;

            var minSize = minimumSize;
            minSize.x = 200;
            minimumSize = minSize;
        }

        /// <inheritdoc />
        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Add Bind Element");

            var currentNameSpace = (string?)null;
            var nameSpaceParent = (AdvancedDropdownItem?)null;

            foreach (var type in _viewTypes)
            {
                var nameSpace = type.Namespace ?? "";
                if (nameSpace != currentNameSpace)
                {
                    currentNameSpace = nameSpace;
                    nameSpaceParent = new AdvancedDropdownItem(currentNameSpace){ icon = IconConst.ScriptIcon };
                    root.AddChild(nameSpaceParent);
                }

                nameSpaceParent?.AddChild(new TypeItem(TypeUtils.GetNestedTypeName(type), type));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeItem bindItem)
            {
                _inis.ItemSelected(bindItem.Type);
            }
        }

        protected class TypeItem : AdvancedDropdownItem
        {
            public readonly Type Type;

            public TypeItem(string name, Type type) : base(name)
            {
                Type = type;
                icon = IconConst.ScriptIcon;
            }
        }
    }
}