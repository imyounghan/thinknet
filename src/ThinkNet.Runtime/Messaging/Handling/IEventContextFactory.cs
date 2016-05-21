using System;
using ThinkLib.Common;
using ThinkLib.Contexts;
using ThinkNet.Configurations;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示创建事件上下文的工厂接口
    /// </summary>
    [UnderlyingComponent(typeof(EventContextFactory))]
    public interface IEventContextFactory : IContextManager
    {
        IEventContext CreateEventContext();

        /// <summary>
        /// 当前上下文
        /// </summary>
        IEventContext GetCurrentEventContext();
    }

    public class EventContextFactory : ContextManager, IEventContextFactory
    {
        class EventContext : DisposableObject, IContext, IEventContext
        {
            private readonly IContextManager _contextManager;
            private readonly object _context;
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public EventContext(IContextManager contextManager, object context)
            {
                this._contextManager = contextManager;
                this._context = context;
            }

            #region IEventContext 成员

            public object Context
            {
                get { return this._context; }
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


            protected override void Dispose(bool disposing)
            {
                using (_context as IDisposable) { }
            }

            IContextManager IContext.ContextManager
            {
                get { return this._contextManager; }
            }    
        }
        
        public EventContextFactory()
            : base("thread")
        {
        }

        protected virtual object CreateDbContext()
        {
            return null;
        }

        #region IEventContextFactory 成员

        public IEventContext CreateEventContext()
        {
            return new EventContext(this, CreateDbContext());
        }


        public IEventContext GetCurrentEventContext()
        {
            return base.CurrentContext.GetContext() as IEventContext;
        }

        #endregion
    }
}
