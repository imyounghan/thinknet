
namespace ThinkNet.Messaging.Handling
{
    public interface IEventContext
    {
        void AddCommand(ICommand command);
    }
}
