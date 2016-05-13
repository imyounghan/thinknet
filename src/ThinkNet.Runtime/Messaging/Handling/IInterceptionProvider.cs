using System;
using System.Collections.Generic;

namespace ThinkNet.Messaging.Handling
{
    public interface IInterceptionProvider
    {
        IEnumerable<IProxyInterception> GetInterceptors(Type type);
    }
}
