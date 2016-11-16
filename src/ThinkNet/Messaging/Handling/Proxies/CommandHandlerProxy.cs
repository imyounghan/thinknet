using System.Reflection;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Proxies
{
    public class CommandHandlerProxy : MessageHandlerProxy
    {
        private readonly CommandContext commandContext;

        public CommandHandlerProxy(IHandler handler, MethodInfo method, InterceptorPipeline pipeline, CommandContext commandContext)
            : base(handler, method, pipeline)
        {
            this.commandContext = commandContext;
        }

        public override void Handle(object[] args)
        {
            if(commandContext != null) {
                var parames = new object[] { commandContext, args[0] };
                base.Handle(parames);
                return;
            }

            base.Handle(args);
        }
        
    }
}
