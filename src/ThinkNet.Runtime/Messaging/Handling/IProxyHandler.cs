
using System;

namespace ThinkNet.Messaging.Handling
{
    public interface IProxyHandler
    {
        void Handle(object message);
        //string MessageId { get; }

        //Type MessageType { get; }

        Type HanderType { get; }

        //IMessage GetInnerMessage();

        //IHandler GetInnerHandler();
    }
}
