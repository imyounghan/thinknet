
namespace ThinkNet.Messaging.Handling
{
    public interface IEventContext : ThinkLib.Common.IUnitOfWork, System.IDisposable
    {
        object Context { get; }

        T GetContext<T>() where T : class;

        void AddCommand(ICommand command);
    }
}
