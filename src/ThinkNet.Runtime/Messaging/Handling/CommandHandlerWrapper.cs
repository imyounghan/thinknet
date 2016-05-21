
namespace ThinkNet.Messaging.Handling
{
    public class CommandHandlerWrapper<TCommand> : MessageHandlerWrapper<TCommand>
        where TCommand : class, ICommand
    {
        private readonly ICommandContextFactory _commandContextFactory;

        public CommandHandlerWrapper(IHandler handler, ICommandContextFactory commandContextFactory)
            : base(handler)
        {
            this._commandContextFactory = commandContextFactory;
        }

        public override void Handle(TCommand command)
        {
            var commandHandler = this.GetInnerHandler() as ICommandHandler<TCommand>;
            if (commandHandler == null)
                return;

            var context = _commandContextFactory.CreateCommandContext();
            commandHandler.Handle(context, command);
            context.Commit(command.Id);
        }
    }
}
