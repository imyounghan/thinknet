using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkLib.Common;
using ThinkLib.Contexts;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示创建事件上下文的工厂接口
    /// </summary>
    [UnderlyingComponent(typeof(DefaultEventContextFactory))]
    public interface IEventContextFactory
    {
        IEventContext CreateEventContext();
    }

    internal class DefaultEventContextFactory : IEventContextFactory
    {
        class EventContext : IEventContext
        {
            #region IEventContext 成员

            public IContext Context
            {
                get { throw new NotImplementedException(); }
            }

            public T GetContext<T>() where T : class
            {
                var context = this.Context as T;

                return context;
            }

            public void AddCommand(ICommand command)
            {
                throw new NotImplementedException();
            }

            #endregion

            #region IUnitOfWork 成员

            public void Commit()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #region IEventContextFactory 成员

        public IEventContext CreateEventContext()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
