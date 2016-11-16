using System;
using System.Reflection;

namespace ThinkNet.Messaging.Handling.Proxies
{
    public interface IHandlerMethodProvider
    {
        MethodInfo GetMethodInfo(Type type, Type[] parameterTypes);
    }
}
