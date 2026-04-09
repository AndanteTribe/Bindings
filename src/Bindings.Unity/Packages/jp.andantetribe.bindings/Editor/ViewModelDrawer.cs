#nullable enable

using Bindings.Editor.Internal;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bindings.Editor
{
    [CustomPropertyDrawer(typeof(IViewModel), true)]
    public class ViewModelDrawer : PropertyDrawer
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
            foldoutCheck.parent.Add(new Label(property.managedReferenceValue.GetType().FullName)
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

            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        break;
                    }

                    var field = new PropertyField(iterator.Copy());
                    field.Bind(property.serializedObject);
                    root.Add(field);
                }
                while (iterator.NextVisible(false));
            }

            return root;
        }
    }
}