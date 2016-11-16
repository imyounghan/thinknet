using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common;
using ThinkNet.Contracts;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime.Executing
{
    public class MessageExecutor : Executor, IInitializer
    {
        private readonly Dictionary<string, IProxyHandler> handlers;

        public MessageExecutor(IEnvelopeSender sender,
            IMessageHandlerRecordStore handlerStore,
            IMessageBus messageBus,
            ICommandResultNotification notification)
            : base()
        {
            this.handlers = new Dictionary<string, IProxyHandler>() {
                { "EventStream", new EventStreamInnerHandler(sender, messageBus, handlerStore) },
                { "CommandResult", new CommandResultRepliedInnerHandler(notification) }
            };
        }


        protected override IEnumerable<IProxyHandler> GetProxyHandlers(Type type)
        {
            if(type == typeof(EventStream)) {
                yield return handlers["EventStream"];
            }

            if(type == typeof(CommandResultReplied)) {
                yield return handlers["CommandResult"];
            }

            throw new NotImplementedException();
        }

        #region IInitializer 成员

        void IInitializer.Initialize(IEnumerable<Type> types)
        {
            handlers.Values.OfType<IInitializer>().ForEach(delegate(IInitializer initializer) {
                initializer.Initialize(types);
            });
        }

        #endregion

        //protected virtual void OnExecuted(TMessage message, ExecutionStatus status)
        //{
        //    if (LogManager.Default.IsDebugEnabled) {
        //        LogManager.Default.DebugFormat("Handle {0} success.", message);
        //    }
        //}

        //protected virtual void OnException(TMessage message, ThinkNetException ex)
        //{
        //    if (LogManager.Default.IsErrorEnabled) {
        //        LogManager.Default.Error(ex, "Exception raised when handling {0}.", message);
        //    }
        //}        
    }
}
