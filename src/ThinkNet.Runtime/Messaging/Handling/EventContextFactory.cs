using System;
using ThinkLib.Common;
using ThinkLib.Contexts;

namespace ThinkNet.Messaging.Handling
{
    public class EventContextFactory : ContextManager, IEventContextFactory
    {
        class EventContext : DisposableObject, IEventContext, IContext
        {

            private readonly IContextManager _contextManager;
            private readonly object _context;
            public EventContext(object context, IContextManager contextManager)
            {
                this._context = context;
                this._contextManager = contextManager;
            }


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
                
            }

            protected override void Dispose(bool disposing)
            {
                using (_context as IDisposable) { }
            }


            public void Commit()
            {
                
            }

            IContextManager IContext.ContextManager
            {
                get { return this._contextManager; }
            }
        }

        private readonly ICommandBus _commandBus;

        public EventContextFactory(ICommandBus commandBus)
            : this(commandBus, "thread")
        { }

        protected EventContextFactory(ICommandBus commandBus, string contextType)
            : base(contextType)
        {
            this._commandBus = commandBus;
        }

        protected virtual object CreateDbContext()
        {
            return null;
        }

        #region IEventContextFactory 成员

        public IEventContext CreateEventContext()
        {
            return new EventContext(CreateDbContext(), this);
        }


        public IEventContext GetEventContext()
        {
            return base.CurrentContext.GetContext() as IEventContext;
        }

        #endregion
    }
}
