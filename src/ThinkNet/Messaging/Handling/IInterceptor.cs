using System;

namespace ThinkNet.Messaging.Handling
{
    public interface IInterceptor<in TMessage> 
        where TMessage : class, IMessage
    {
        void OnHandling(TMessage message);

        void OnHandled(AggregateException exception, TMessage message);
    }
}
