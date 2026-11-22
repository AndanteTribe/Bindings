#nullable enable

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bindings.Editor.Internal
{
    [CustomPropertyDrawer(typeof(TempViewModel))]
    internal class TempViewModelDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new Foldout
            {
                focusable = true,
                style = { paddingTop = 5f, paddingBottom = 5f, paddingLeft = 5f, paddingRight = 5f },
            };
            var foldoutCheck = root.Q(className: Foldout.checkmarkUssClassName);
            foldoutCheck.parent.Add(new Image
            {
                image = IconConst.ScriptIcon,
                style = { width = 18f, height = 18f }
            });
            var preview = (TempViewModel)property.managedReferenceValue;
            foldoutCheck.parent.Add(new Label(preview.ViewModelType.FullName)
            {
                style = { marginLeft = 5f, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13 }
            });
            root.Add(new VisualElement()
            {
                style =
                {
                    height = 2,
                    backgroundColor = Color.gray,
                    marginBottom = 5,
                }
            });

            var slots = property.FindPropertyRelative(nameof(TempViewModel._slots));
            for (var i = 0; i < slots.arraySize; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                var value = slot.FindPropertyRelative(nameof(Slot<int>.Value));
                var label = ObjectNames.NicifyVariableName(((ISlot)slot.managedReferenceValue).MemberName);
                var field = new PropertyField(value, label);
                field.Bind(property.serializedObject);
                root.Add(field);
            }

            return root;
        }
    }
}