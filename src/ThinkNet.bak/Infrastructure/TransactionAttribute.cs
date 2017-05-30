using System;
using System.Linq;
using ThinkNet.Database;
using ThinkNet.Database.Context;
using ThinkNet.Infrastructure.Interception;
using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 事务
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TransactionAttribute : InterceptorAttribute
    {
        class TransactionInterceptor : IInterceptor
        {
            private readonly IDataContextFactory dataContextFactory;
            private readonly IMessageBus messageBus;

            public TransactionInterceptor(IDataContextFactory dataContextFactory, IMessageBus messageBus)
            {
                this.dataContextFactory = dataContextFactory;
                this.messageBus = messageBus;
            }

            #region IInterceptor 成员

            public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptorDelegate getNext)
            {
                using(var dataContext = dataContextFactory.Create()) {
                    var context = dataContext as IContext;
                    CurrentContext.Bind(context);
                    var methodReturn = getNext().Invoke(input, getNext);
                    CurrentContext.Unbind(context.ContextManager);

                   var events = dataContext.TrackingObjects.OfType<IEventPublisher>()
                       .SelectMany(item => item.Events).ToArray();
                   messageBus.PublishAsync(events);

                    return methodReturn;

                }
            }

            #endregion
        }

        /// <summary>
        /// 创建事务的拦截器
        /// </summary>
        public override IInterceptor CreateInterceptor(IObjectContainer container)
        {
            return new TransactionInterceptor(container.Resolve<IDataContextFactory>(), 
                container.Resolve<IMessageBus>());
        }
    }
}
