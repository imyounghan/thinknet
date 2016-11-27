using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 命令结果的内部处理器
    /// </summary>
    public class CommandResultInnerHandler : IHandlerAgent, IMessageHandler<CommandResult>
    {
        private readonly ICommandResultNotification _notification;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        /// <param name="notification"></param>
        public CommandResultInnerHandler(ICommandResultNotification notification)            
        {
            this._notification = notification;
        }

        public object GetInnerHandler()
        {
            return this;
        }

        public void Handle(object[] args)
        {            
            var reply = args[0] as CommandResult;

            this.Handle(reply);
        }       

        private void Handle(CommandResult result)
        {
            switch (result.CommandReturnType) {
                case CommandReturnType.CommandExecuted:
                    _notification.NotifyCommandHandled(result);
                    break;
                case CommandReturnType.DomainEventHandled:
                    _notification.NotifyEventHandled(result);
                    break;
            }
        }

        #region IMessageHandler<CommandResult> 成员

        void IMessageHandler<CommandResult>.Handle(CommandResult commandResult)
        {
            this.Handle(commandResult);
        }

        #endregion
    }
}
