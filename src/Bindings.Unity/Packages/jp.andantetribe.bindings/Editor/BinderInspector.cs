#nullable enable

using System;
using System.Runtime.CompilerServices;
using Bindings.Editor.Internal;
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
        private TempViewModelContainer? _viewModelContainer;

        /// <inheritdoc />
        public override VisualElement CreateInspectorGUI()
        {
            _viewsProperty = serializedObject.FindProperty("_views");

            var root = new VisualElement
            {
                style = { paddingLeft = 3f, paddingRight = 3f, paddingTop = 3f, paddingBottom = 3f }
            };
            root.Add(new PropertyField(serializedObject.FindProperty("_runOnStart")));
            root.Add(CreateViewsField());
            root.Add(CreatePreviewField());
            return root;
        }

        protected virtual void OnDestroy()
        {
            if (_viewModelContainer != null)
            {
                DestroyImmediate(_viewModelContainer);
            }
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

        private VisualElement CreateViewsField()
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
            addButton.RegisterCallback<ClickEvent, AdvancedViewDropdown>(static (evt, dropdown) =>
            {
                var button = (Button)evt.target;
                dropdown.Show(button.worldBound);
            }, new AdvancedViewDropdown(this));

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

        private VisualElement CreatePreviewField()
        {
            var previewButton = new Button()
            {
                text = "Preview",
                style =
                {
                    flexGrow = 1f,
                    marginTop = 5f,
                    marginBottom = 5f,
                }
            };
            previewButton.RegisterCallback<ClickEvent, BinderInspector>(static (evt, self) =>
            {
                if (self._viewsProperty == null)
                {
                    return;
                }

                AnimationMode.StartAnimationMode();

                var button = (Button)evt.target;
                var root = button.parent;
                root.Remove(button);

                self._viewModelContainer ??= TempViewModelContainer.Create();

                var viewModels = new ListView
                {
                    bindingPath = "_viewModels",
                    reorderMode = ListViewReorderMode.Animated,
                    showBorder = true,
                    showAddRemoveFooter = false,
                    showFoldoutHeader = false,
                    showBoundCollectionSize = false,
                    virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                    selectionType = SelectionType.None,
                    showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                };
                viewModels.Bind(self._viewModelContainer.Build(self._viewsProperty, self.target));

                var invokeButton = new Button()
                {
                    text = "Invoke",
                    style =
                    {
                        width = 200f,
                        alignSelf = Align.Center,
                    }
                };
                invokeButton.RegisterCallback<ClickEvent, BinderInspector>(static (_, self) =>
                {
                    self._viewModelContainer?.BindTo(self.target);
                    SceneView.RepaintAll();
                }, self);

                var box = CreateBox("Preview");
                box.Add(viewModels);
                box.Add(invokeButton);
                root.Add(box);
            }, this);
            return previewButton;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static VisualElement CreateBox(string label)
        {
            var box = new Box
            {
                style =
                {
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