using ThinkNet.Common;
using ThinkNet.Common.Interception;
using ThinkNet.Contracts;
using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 过滤处理过的消息的拦截器
    /// </summary>
    public class FilterHandledMessageInterceptor : IInterceptor
    {
        private readonly IHandlerRecordStore _handlerStore;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public FilterHandledMessageInterceptor(IHandlerRecordStore handlerStore)
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
            var uniquely = input.Arguments[parameter.Name] as IUniquelyIdentifiable;            
            
            if(uniquely == null) {
                return getNext().Invoke(input, getNext);
            }
            

            var messageType = parameter.GetType();
            var handlerType = input.Target.GetType();

            if (_handlerStore.HandlerIsExecuted(uniquely.UniqueId, messageType, handlerType)) {
                var errorMessage = string.Format("The message has been handled. MessageHandlerType:{0}, MessageType:{1}, MessageId:{2}.",
                    handlerType.FullName, messageType.FullName, uniquely.UniqueId);
                //throw new ThinkNetException(errorMessage);
                return new MethodReturn(input, new ThinkNetException(errorMessage));
            }

            var methodReturn = getNext().Invoke(input, getNext);

            if (methodReturn.Exception != null)
                _handlerStore.AddHandlerInfo(uniquely.UniqueId, messageType, handlerType);

            return methodReturn;
        }

        #endregion
    }
}
