#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Bindings.Editor.Internal
{
    internal class TempViewModelContainer : ScriptableObject
    {
        [SerializeReference]
        private IViewModel[] _viewModels = Array.Empty<IViewModel>();

        private static readonly Dictionary<Type, Type> s_genericViewsTypes = TypeUtils.GetAllGenericViewTypes();

        public static TempViewModelContainer Create() => CreateInstance<TempViewModelContainer>();

        public SerializedObject Build(SerializedProperty viewsProperty, object binder)
        {
            var size = viewsProperty.arraySize;
            if (_viewModels.Length != size)
            {
                _viewModels = new IViewModel[size];
            }
            var viewModels = _viewModels.AsSpan();
            for (var i = 0; i < viewModels.Length; i++)
            {
                var view = viewsProperty.GetArrayElementAtIndex(i).managedReferenceValue;
                if (view?.GetType() is not { } viewType || !s_genericViewsTypes.TryGetValue(viewType, out var type))
                {
                    continue;
                }
                viewModels[i] = (IViewModel)Activator.CreateInstance(type, GetConstructorArguments(type, binder));
            }
            return new SerializedObject(this);
        }

        public void BindTo(object binder)
        {
            var publisher = (IMvvmPublisher)binder;
            foreach (var viewModel in _viewModels.AsSpan())
            {
                publisher.Publish(new DebugBindMessage(viewModel));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object[] GetConstructorArguments(Type type, object binder)
        {
            var parameters = type.GetConstructors().Single().GetParameters();
            var arguments = new object[parameters.Length];
            for (var i = 0; i < parameters.Length - 1; i++)
            {
#if NET8_0_OR_GREATER
                arguments[i] = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(parameters[i].ParameterType);
#else
                arguments[i] = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(parameters[i].ParameterType);
#endif
            }
            arguments[^1] = binder;
            return arguments;
        }
    }
}