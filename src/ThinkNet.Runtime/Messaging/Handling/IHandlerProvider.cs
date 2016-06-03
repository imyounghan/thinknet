using System;
using System.Collections.Generic;

namespace ThinkNet.Messaging.Handling
{
    public interface IHandlerProvider
    {
        IEnumerable<IProxyHandler> GetHandlers(Type type);

        IProxyHandler GetCommandHandler(Type type);

        IProxyHandler GetEventHandler(Type type);
    }
}
