using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 命令结果的内部处理程序
    /// </summary>
    public class CommandResultInnerHandler : HandlerAgent//, IMessageHandler<CommandResult>
    {
        private readonly ICommandResultNotification _notification;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public CommandResultInnerHandler(ICommandResultNotification notification)
        {
            this._notification = notification;
        }

        /// <summary>
        /// 获取处理命令结果的程序
        /// </summary>
        public override object GetInnerHandler()
        {
            return this;
        }

        /// <summary>
        /// 处理命令结果
        /// </summary>
        protected override void TryHandle(object[] args)
        {
            var result = args[0] as CommandResult;

            _notification.Notify(result.CommandId, result, result.CommandReturnType);

            //switch (result.CommandReturnType) {
            //    case CommandReturnType.CommandExecuted:
            //        _notification.NotifyCommandHandled(result);
            //        break;
            //    case CommandReturnType.DomainEventHandled:
            //        _notification.NotifyEventHandled(result);
            //        break;
            //}
        }

        //#region IMessageHandler<CommandResult> 成员

        //void IMessageHandler<CommandResult>.Handle(CommandResult commandResult)
        //{
        //    this.TryHandle(commandResult);
        //}

        //#endregion
    }
}
