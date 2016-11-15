namespace ThinkNet.Messaging.Handling
{
    public interface ICommandFilter
    {
        void OnCommandHandled(CommandHandledContext filterContext);

        void OnCommandHandling(CommandHandlingContext filterContext);
    }
}
