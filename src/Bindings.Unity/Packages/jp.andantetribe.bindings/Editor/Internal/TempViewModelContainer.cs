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
        private TempViewModel[] _viewModels = Array.Empty<TempViewModel>();

        private static readonly Dictionary<Type, Type> s_genericViewsTypes = TypeUtils.GetAllGenericViewTypes();

        public static TempViewModelContainer Create() => CreateInstance<TempViewModelContainer>();

        public SerializedObject Build(SerializedProperty viewsProperty, IMvvmPublisher publisher)
        {
            _viewModels = new TempViewModel[viewsProperty.arraySize];
            var viewModels = _viewModels.AsSpan();
            for (var i = 0; i < viewModels.Length; i++)
            {
                var view = viewsProperty.GetArrayElementAtIndex(i);
                viewModels[i] = new TempViewModel(s_genericViewsTypes[view.managedReferenceValue.GetType()]);
            }
            var serialized = new SerializedObject(this);
            var models = serialized.FindProperty(nameof(_viewModels));
            for (var i = 0; i < viewModels.Length; i++)
            {
                var slots = models.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(TempViewModel._slots));
                viewModels[i].LoadDefaults(slots, publisher);
            }
            serialized.Update();
            return serialized;
        }

        public void BindTo(SerializedProperty viewsProperty, IMvvmPublisher publisher)
        {
            var viewModels = _viewModels.AsSpan();
            if (viewsProperty.arraySize != viewModels.Length)
            {
                throw new InvalidOperationException("The Views list changed. Reopen the preview.");
            }
            for (var i = 0; i < viewModels.Length; i++)
            {
                var view = viewsProperty.GetArrayElementAtIndex(i);
                if (view.managedReferenceValue is IMvvmSubscriber<DebugBindMessage> subscriber)
                {
                    subscriber.OnReceivedMessage(new DebugBindMessage(viewModels[i].CreateViewModel(publisher)));
                }
            }
        }
    }
}