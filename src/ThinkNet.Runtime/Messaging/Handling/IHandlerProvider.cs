using System;
using System.Collections.Generic;

namespace ThinkNet.Messaging.Handling
{
    public interface IHandlerProvider
    {
        IEnumerable<IProxyHandler> GetMessageHandlers(Type type);

        IProxyHandler GetCommandHandler(Type type);

        IProxyHandler GetEventHandler(IEnumerable<Type> types);
    }
}
