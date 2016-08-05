using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling
{
    public class EventContextFactory : IEventContextFactory
    {
        class EventContext : DisposableObject, IEventContext
        {
            private readonly List<ICommand> _commands;
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICommandBus _commandBus;
            public EventContext(IUnitOfWork unitOfWork, ICommandBus commandBus)
            {
                this._unitOfWork = unitOfWork;
                this._commandBus = commandBus;
                this._commands = new List<ICommand>();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    using (_unitOfWork as IDisposable) { }
            }

            public IUnitOfWork UnitOfWork
            {
                get { return this._unitOfWork; }
            }

            public T GetDbContext<T>() where T : class
            {
                return _unitOfWork as T;
            }

            public void AddCommand(ICommand command)
            {
                _commands.Add(command);
            }
            

            public void Commit()
            {
                try {
                    _unitOfWork.Commit();
                }
                catch (Exception) {
                    _unitOfWork.Rollback();
                    throw;
                }
                _commandBus.Send(_commands);
                _commands.Clear();
            }

            public void Rollback()
            {
                _unitOfWork.Rollback();
            }
        }

        class EmptyUnitOfWork : IUnitOfWork
        {
            public readonly static EmptyUnitOfWork Instance = new EmptyUnitOfWork();
            private EmptyUnitOfWork()
            { }

            public void Commit()
            { }

            public void Rollback()
            { }
        }

        private readonly ICommandBus _commandBus;

        public EventContextFactory(ICommandBus commandBus)
        {
            this._commandBus = commandBus;
        }



        protected virtual IUnitOfWork CreateUnitOfWork()
        {
            return EmptyUnitOfWork.Instance;
        }


        [ThreadStatic]
        private static EventContext currentContext;
        public IEventContext GetEventContext()
        {
            return currentContext;
        }
        
        public void Bind()
        {
            currentContext = new EventContext(CreateUnitOfWork(), _commandBus);
        }

        public void Unbind(bool success)
        {
            if (success)
                currentContext.Commit();
            else
                currentContext.Rollback();


            using (currentContext) {
                currentContext = null;
            }
        }
    }
}
