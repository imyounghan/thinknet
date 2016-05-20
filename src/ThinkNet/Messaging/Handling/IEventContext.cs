
namespace ThinkNet.Messaging.Handling
{
    public interface IEventContext
    {
        object Context { get; }

        T GetContext<T>() where T : class;

        void AddCommand(ICommand command);
    }
}
