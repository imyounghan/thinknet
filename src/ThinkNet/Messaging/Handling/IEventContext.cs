using ThinkLib.Common;
using ThinkLib.Contexts;

namespace ThinkNet.Messaging.Handling
{
    public interface IEventContext : IUnitOfWork
    {
        IContext Context { get; }

        T GetContext<T>();

        void AddCommand(ICommand command);
    }
}
