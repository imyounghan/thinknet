using System;

using ThinkNet.Infrastructure;
using ThinkNet.Runtime.Serialization;


namespace ThinkNet.Messaging.Runtime
{
    /// <summary>
    /// 命令处理器
    /// </summary>
    public class DefaultCommandProcessor : MessageProcessor<ICommand>, ICommandProcessor
    {
        private readonly ICommandResultManager _commandResultManager;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public DefaultCommandProcessor(ICommandExecutor commandExecutor,
            IMessageStore messageStore, 
            ITextSerializer serializer, 
            ICommandResultManager commandResultManager)
            : base("Command", 4, commandExecutor, messageStore, serializer)
        {
            this._commandResultManager = commandResultManager;
        }

        protected override void Process(ICommand command)
        {
            try {
                base.Process(command);
                _commandResultManager.NotifyCommandExecuted(command.Id, CommandStatus.Success, null);
            }
            catch (Exception ex) {
                _commandResultManager.NotifyCommandCompleted(command.Id, CommandStatus.Failed, ex);
                throw ex;
            }
        }
    }
}
