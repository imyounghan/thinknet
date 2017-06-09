

namespace UserRegistration
{
    using System.Collections;
    using System.ComponentModel.Composition;
    using System.ServiceModel;

    using ThinkNet.Communication;
    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging;

    [Export(typeof(ICommandService))]
    public class CommandService : ICommandService
    {

        private readonly ChannelFactory<IRequest> channelFactory;

        private readonly ITextSerializer serializer;

        public CommandService()
        {
            this.channelFactory = new ChannelFactory<IRequest>(new NetTcpBinding(), "net.tcp://127.0.0.1:9999/Request");
            this.serializer = new DefaultTextSerializer();
        }

        #region ICommandService 成员

        public ICommandResult Send(ICommand command)
        {
            var commandName = command.GetType().Name;
            var commandData = serializer.Serialize(command);

            var response = channelFactory.CreateChannel().Send(commandName, commandData);

            var commandResult = new CommandResult {
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage,
                Status = (ExecutionStatus)response.Status,
            };
            if (!string.IsNullOrEmpty(response.Result))
            {
                commandResult.ErrorData = serializer.Deserialize<IDictionary>(response.Result);
            }

            return commandResult;
        }

        public ICommandResult Execute(ICommand command)
        {
            var commandName = command.GetType().Name;
            var commandData = serializer.Serialize(command);
            
            var response = channelFactory.CreateChannel().Execute(commandName, commandData);

            var commandResult = new CommandResult {
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage,
                Status = (ExecutionStatus)response.Status,
            };
            if(!string.IsNullOrEmpty(response.Result)) {
                commandResult.ErrorData = serializer.Deserialize<Hashtable>(response.Result);
            }

            return commandResult;
        }

        #endregion
    }
}
