using System;
using ThinkLib.Common;
using ThinkLib.Contexts;

namespace ThinkNet.Messaging.Handling
{
    public interface IEventContext : IUnitOfWork, IContext, IDisposable
    {
        object Context { get; }

        T GetContext<T>() where T : class;

        void AddCommand(ICommand command);
    }
}
