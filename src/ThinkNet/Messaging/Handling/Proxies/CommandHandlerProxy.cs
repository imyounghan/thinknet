using System.Reflection;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Proxies
{
    /// <summary>
    /// 命令处理的代理器
    /// </summary>
    public class CommandHandlerProxy : MessageHandlerProxy
    {
        private readonly CommandContext commandContext;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public CommandHandlerProxy(object handler, MethodInfo method, InterceptorPipeline pipeline, CommandContext commandContext)
            : base(handler, method, pipeline)
        {
            this.commandContext = commandContext;
        }

        /// <summary>
        /// 处理命令
        /// </summary>
        public override void Handle(object[] args)
        {
            if(commandContext != null) {
                base.Handle(new object[] { commandContext, args[0] });
                return;
            }

            base.Handle(args);
        }
        
    }
}
