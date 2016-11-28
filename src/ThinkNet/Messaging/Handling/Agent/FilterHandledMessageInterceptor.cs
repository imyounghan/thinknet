using ThinkLib;
using ThinkLib.Interception;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 过滤处理过的消息的拦截器
    /// </summary>
    public class FilterHandledMessageInterceptor : IInterceptor
    {
        private readonly IMessageHandlerRecordStore _handlerStore;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public FilterHandledMessageInterceptor(IMessageHandlerRecordStore handlerStore)
        {
            this._handlerStore = handlerStore;
        }


        #region IInterceptor 成员
        /// <summary>
        /// 创建消息是否处理的拦截器
        /// </summary>
        public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptorDelegate getNext)
        {
            var parameter = input.Arguments.GetParameterInfo(input.Arguments.Count - 1);
            var argument = input.Arguments[parameter.Name];            

            var uniquely = argument as IUniquelyIdentifiable;            
            
            if(uniquely == null || !(argument is IMessage)) {
                return getNext().Invoke(input, getNext);
            }            

            var eventStream = argument as EventStream;
            if(eventStream != null && eventStream.Events.IsEmpty()) {
                input.InvocationContext["CommandReturnType"] = CommandReturnType.DomainEventHandled;
                input.InvocationContext["CommandStatus"] = CommandStatus.NothingChanged;
                input.InvocationContext["CommandId"] = eventStream.CorrelationId;
                
                return new MethodReturn(input, new ThinkNetException(""));
            }            

            var messageType = parameter.GetType();
            var handlerType = input.Target.GetType();

            if (_handlerStore.HandlerIsExecuted(uniquely.Id, messageType, handlerType)) {
                var errorMessage = string.Format("The message has been handled. MessageHandlerType:{0}, MessageType:{1}, MessageId:{2}.",
                    handlerType.FullName, messageType.FullName, uniquely.Id);
                //throw new ThinkNetException(errorMessage);
                return new MethodReturn(input, new ThinkNetException(errorMessage));
            }

            var methodReturn = getNext().Invoke(input, getNext);

            if (methodReturn.Exception != null)
                _handlerStore.AddHandlerInfo(uniquely.Id, messageType, handlerType);

            return methodReturn;
        }

        #endregion
    }
}
