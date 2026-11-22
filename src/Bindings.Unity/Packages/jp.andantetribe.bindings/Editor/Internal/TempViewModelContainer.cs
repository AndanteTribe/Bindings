#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bindings.Editor.Internal
{
    internal class TempViewModelContainer : ScriptableObject
    {
        [SerializeReference]
        private TempViewModel?[] _viewModels = Array.Empty<TempViewModel?>();

        private static readonly Dictionary<Type, Type> s_genericViewsTypes = TypeUtils.GetAllGenericViewTypes();

        public static TempViewModelContainer Create() => CreateInstance<TempViewModelContainer>();

        public SerializedObject Build(SerializedProperty viewsProperty, IMvvmPublisher publisher)
        {
            _viewModels = new TempViewModel?[viewsProperty.arraySize];
            var viewModels = _viewModels.AsSpan();
            for (var i = 0; i < viewModels.Length; i++)
            {
                var view = viewsProperty.GetArrayElementAtIndex(i).managedReferenceValue;
                if (view != null && s_genericViewsTypes.TryGetValue(view.GetType(), out var modelType))
                {
                    viewModels[i] = new TempViewModel(modelType);
                }
            }
            var serialized = new SerializedObject(this);
            var models = serialized.FindProperty(nameof(_viewModels));
            for (var i = 0; i < viewModels.Length; i++)
            {
                if (viewModels[i] is { } viewModel)
                {
                    var slots = models.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(TempViewModel._slots));
                    viewModel.LoadDefaults(slots, publisher);
                }
            }
            serialized.Update();
            return serialized;
        }

        public void BindTo(SerializedProperty viewsProperty, IMvvmPublisher publisher)
        {
            var viewModels = _viewModels.AsSpan();
            for (var i = 0; i < viewModels.Length; i++)
            {
                if (viewModels[i] is { } viewModel
                    && viewsProperty.GetArrayElementAtIndex(i).managedReferenceValue is IMvvmSubscriber<DebugBindMessage> subscriber)
                {
                    subscriber.OnReceivedMessage(new DebugBindMessage(viewModel.CreateViewModel(publisher)));
                }
            }
        }
    }
}