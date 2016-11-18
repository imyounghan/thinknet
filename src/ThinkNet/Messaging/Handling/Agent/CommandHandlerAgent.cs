using System.Reflection;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 命令处理的代理器
    /// </summary>
    public class CommandHandlerAgent : MessageHandlerAgent
    {
        private readonly CommandContext commandContext;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public CommandHandlerAgent(object handler, MethodInfo method, InterceptorPipeline pipeline, CommandContext commandContext)
            : base(handler, method, pipeline)
        {
            this.commandContext = commandContext;
        }

        /// <summary>
        /// 处理命令
        /// </summary>
        /// <param name="args"></param>
        public override void Handle(object[] args)
        {
            if (commandContext != null) {
                args = new object[] { commandContext, args[0] };
            }

            base.Handle(args);
        }
    }
}
