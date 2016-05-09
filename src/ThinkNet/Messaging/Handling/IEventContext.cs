using ThinkLib.Contexts;

namespace ThinkNet.Messaging.Handling
{
    public interface IEventContext
    {
        IContext Context { get; }

        T GetContext<T>();

        void AddCommand(ICommand command);
    }
}
