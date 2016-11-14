using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling
{
    public class CommandHandlerWrapper : MessageHandlerWrapper
    {
        private readonly CommandContext commandContext;

        public CommandHandlerWrapper(IHandler handler, Type contractType, CommandContext commandContext)
            : base(handler, contractType)
        {
            this.commandContext = commandContext;
        }

        protected override MethodInfo GetHandleMethodInfo(Type targetType, Type[] parameterTypes)
        {
            var types = new List<Type>(parameterTypes);
            types.Insert(0, typeof(ICommandContext));

            return targetType.GetMethod("Handle", types.ToArray());
        }

        public override void Handle(object handler, object[] args)
        {
            InterceptorPipeline pipeline;
            var method = GetHandleMethodInfo(out pipeline);

            var command = args.First() as ICommand;

            var parames = new object[] { commandContext, command };

            if (pipeline.Count == 0) {
                method.Invoke(handler, parames);
                commandContext.Commit(command.Id);
            }
            else {
                var input = new MethodInvocation(handler, method, parames);
                pipeline.Invoke(input, delegate {
                    method.Invoke(handler, parames);
                    commandContext.Commit(command.Id);
                    return new MethodReturn(input, null, parames);
                });
            }
            //var message = args.First();

            //((dynamic)handler).Handle((dynamic)_commandContext, (dynamic)message);

            //_commandContext.Commit(((dynamic)message).Id);
        }
    }
}
