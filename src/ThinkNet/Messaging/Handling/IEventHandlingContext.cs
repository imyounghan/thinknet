
namespace ThinkNet.Messaging.Handling
{
    public interface IEventHandlingContext
    {
        void AddCommand(ICommand command);
    }
}
