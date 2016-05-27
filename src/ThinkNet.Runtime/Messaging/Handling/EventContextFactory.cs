using System;
using ThinkLib.Common;

namespace ThinkNet.Messaging.Handling
{
    public class EventContextFactory : IEventContextFactory
    {
        class EventContext : DisposableObject, IEventContext
        {
            private readonly object _context;
            //private readonly IList<IEvent> pendingEvents;
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public EventContext(object context)
            {
                this._context = context;
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
                throw new NotImplementedException();
            }

            protected override void Dispose(bool disposing)
            {
                using (_context as IDisposable) { }
            }


            public void Commit()
            {
                
            }
        }

        private readonly ICommandBus _commandBus;

        protected virtual object CreateDbContext()
        {
            return null;
        }

        #region IEventContextFactory 成员

        public IEventContext CreateEventContext()
        {
            return new EventContext(CreateDbContext());
        }
        #endregion
    }
}
