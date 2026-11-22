#nullable enable

using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace Bindings.Editor.Internal
{
    internal sealed class InterfacePreviewService : RealProxy
    {
        private InterfacePreviewService(Type serviceType) : base(serviceType) { }

        public static object Create(Type serviceType) => new InterfacePreviewService(serviceType).GetTransparentProxy();

        public override IMessage Invoke(IMessage message)
        {
            var call = (IMethodCallMessage)message;
            var returnType = ((MethodInfo)call.MethodBase).ReturnType;
            var result = returnType != typeof(void) && returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
            return new ReturnMessage(result, call.Args, call.ArgCount, call.LogicalCallContext, call);
        }
    }
}