#nullable enable

using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bindings.Editor
{
    [CustomEditor(typeof(Binder))]
    public class BinderInspector : UnityEditor.Editor, INotifyItemSelected
    {
        private SerializedProperty? _viewsProperty;

        /// <inheritdoc />
        public override VisualElement CreateInspectorGUI()
        {
            _viewsProperty = serializedObject.FindProperty("_views");

            var root = new VisualElement
            {
                style = { paddingLeft = 3f, paddingRight = 3f, paddingTop = 3f, paddingBottom = 3f }
            };
            root.Add(new PropertyField(serializedObject.FindProperty("_runOnEnable")));
            root.Add(CreateViewsField(new AdvancedViewDropdown(this)));
            return root;
        }

        /// <inheritdoc />
        public virtual void ItemSelected(Type type)
        {
            if (_viewsProperty != null)
            {
                var last = _viewsProperty.arraySize;
                _viewsProperty.InsertArrayElementAtIndex(_viewsProperty.arraySize);
                var property = _viewsProperty.GetArrayElementAtIndex(last);
                property.managedReferenceValue = Activator.CreateInstance(type);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static VisualElement CreateViewsField(AdvancedViewDropdown addBindElementDropdown)
        {
            var addButton = new Button()
            {
                text = "Add View",
                style =
                {
                    width = 200f,
                    alignSelf = Align.Center
                }
            };
            addButton.RegisterCallback<ClickEvent, AdvancedViewDropdown>(static (eve, dropdown) =>
            {
                var button = (Button)eve.target;
                dropdown.Show(button.worldBound);
            }, addBindElementDropdown);

            var box = CreateBox("Views");
            box.Add(new ListView
            {
                bindingPath = "_views",
                reorderMode = ListViewReorderMode.Animated,
                showBorder = true,
                showAddRemoveFooter = false,
                showFoldoutHeader = false,
                showBoundCollectionSize = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            });
            box.Add(addButton);
            return box;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static VisualElement CreateBox(string label)
        {
            var box = new Box
            {
                style = {
                    marginTop = 6f,
                    marginBottom = 2f,
                    paddingLeft = 4f,
                    alignItems = Align.Stretch,
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1f,
                }
            };
            box.Add(new Label(label)
            {
                style =
                {
                    marginTop = 5f,
                    marginBottom = 3f,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            });
            return box;
        }
    }
}