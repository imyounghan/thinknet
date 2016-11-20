using System;
using System.Reflection;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 命令处理的代理器
    /// </summary>
    public class CommandHandlerAgent : MessageHandlerAgent
    {
        private readonly Func<CommandContext> commandContextFactory;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public CommandHandlerAgent(object handler, MethodInfo method, 
            InterceptorPipeline pipeline,
            Func<CommandContext> commandContextFactory)
            : base(handler, method, pipeline)
        {
            this.commandContextFactory = commandContextFactory;
        }

        /// <summary>
        /// 处理命令
        /// </summary>
        /// <param name="args"></param>
        public override void Handle(object[] args)
        {
            if (commandContextFactory != null) {
                args = new object[] { commandContextFactory.Invoke(), args[0] };
            }

            base.Handle(args);
        }

        protected override void TryHandle(object[] args)
        {
            base.TryHandle(args);

            if(args.Length == 2) {
                var commandContext = args[0] as CommandContext;
                var command = args[1] as Command;

                commandContext.Commit(command.UniqueId);
            }
        }
    }
}
