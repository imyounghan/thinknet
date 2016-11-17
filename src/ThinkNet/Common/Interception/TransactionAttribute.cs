using System;
using ThinkNet.Common.Composition;
using ThinkNet.Common.Context;
using ThinkNet.Database;

namespace ThinkNet.Common.Interception
{
    /// <summary>
    /// 事务
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TransactionAttribute : InterceptorAttribute
    {
        class TransactionInterceptor : IInterceptor
        {
            public readonly IContext _context;

            public TransactionInterceptor(IContext context)
            {
                this._context = context;
            }

            #region IInterceptor 成员

            public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptorDelegate getNext)
            {
                CurrentContext.Bind(_context);

                var methodReturn = getNext().Invoke(input, getNext);

                using (CurrentContext.Unbind(_context.ContextManager) as IDisposable) 
                { }

                return methodReturn;
            }

            #endregion
        }

        /// <summary>
        /// 创建事务的拦截器
        /// </summary>
        public override IInterceptor CreateInterceptor(IObjectContainer container)
        {
            var dataContextFactory = container.Resolve<IDataContextFactory>();

            return new TransactionInterceptor(dataContextFactory.Create() as IContext);
        }
    }
}
