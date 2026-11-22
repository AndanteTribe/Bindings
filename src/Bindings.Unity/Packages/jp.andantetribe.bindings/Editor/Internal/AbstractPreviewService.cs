#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace Bindings.Editor.Internal
{
    internal static class AbstractPreviewService
    {
        private static readonly ModuleBuilder s_module = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("Bindings.Editor.PreviewServices"), AssemblyBuilderAccess.Run).DefineDynamicModule("PreviewServices");
        private static readonly Dictionary<Type, Type> s_types = new();

        public static object Create(Type service)
        {
            if (!s_types.TryGetValue(service, out var type))
            {
                var builder = s_module.DefineType("Preview_" + Guid.NewGuid().ToString("N"), TypeAttributes.Sealed, service);
                // Instances are allocated without running service constructors.
                var ctor = builder.DefineConstructor(MethodAttributes.Private, CallingConventions.Standard, Type.EmptyTypes).GetILGenerator();
                ctor.Emit(OpCodes.Newobj, typeof(NotSupportedException).GetConstructor(Type.EmptyTypes)!);
                ctor.Emit(OpCodes.Throw);

                foreach (var method in service.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).AsSpan())
                {
                    if (!method.IsVirtual || method.IsFinal || method.DeclaringType == typeof(object))
                    {
                        continue;
                    }
                    if (method.IsGenericMethod || method.ReturnType.IsByRef || method.ReturnType.IsPointer)
                    {
                        throw new NotSupportedException($"Preview cannot stub '{service}.{method.Name}'.");
                    }
                    var parameters = method.GetParameters();
                    var stub = builder.DefineMethod(method.Name,
                        (method.Attributes & MethodAttributes.MemberAccessMask) | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                        method.ReturnType, Array.ConvertAll(parameters, static p => p.ParameterType));
                    var il = stub.GetILGenerator();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].IsOut && parameters[i].ParameterType.IsByRef)
                        {
                            il.Emit(OpCodes.Ldarg, (short)(i + 1));
                            il.Emit(OpCodes.Initobj, parameters[i].ParameterType.GetElementType()!);
                        }
                    }
                    if (method.ReturnType != typeof(void))
                    {
                        il.Emit(OpCodes.Ldloc, il.DeclareLocal(method.ReturnType));
                    }
                    il.Emit(OpCodes.Ret);
                    builder.DefineMethodOverride(stub, method);
                }
                type = builder.CreateType()!;
                s_types.Add(service, type);
            }
            return FormatterServices.GetUninitializedObject(type);
        }
    }
}