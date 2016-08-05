using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling
{
    public interface IEventContext
    {
        IUnitOfWork UnitOfWork { get; }

        T GetDbContext<T>() where T : class;

        void AddCommand(ICommand command);
    }
}
