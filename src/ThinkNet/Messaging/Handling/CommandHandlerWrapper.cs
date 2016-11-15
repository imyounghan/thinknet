using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
            //InterceptorPipeline pipeline;
            //var method = GetHandleMethodInfo(out pipeline);

            //var command = args.First() as ICommand;

            //var parames = new object[] { commandContext, command };

            //if (pipeline.Count == 0) {
            //    method.Invoke(handler, parames);
            //    commandContext.Commit(command.Id);
            //}
            //else {
            //    var input = new MethodInvocation(handler, method, parames);
            //    pipeline.Invoke(input, delegate {
            //        method.Invoke(handler, parames);
            //        commandContext.Commit(command.Id);
            //        return new MethodReturn(input, null, parames);
            //    });
            //}
            //var message = args.First();

            //((dynamic)handler).Handle((dynamic)_commandContext, (dynamic)message);

            //_commandContext.Commit(((dynamic)message).Id);
        }

        protected virtual CommandHandledContext InvokeHandlerMethodFilters(IList<ICommandFilter> filters, object[] parameters)
        {
            CommandHandlingContext preContext = new CommandHandlingContext();
            Func<CommandHandledContext> continuation = () => {
                GetHandleMethodInfo().Invoke(GetTargetHandler(), parameters);
                return new CommandHandledContext();
            };

            // need to reverse the filter list because the continuations are built up backward
            Func<CommandHandledContext> thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokeHandlerMethodFilter(filter, preContext, next));
            return thunk();
        }

        private static CommandHandledContext InvokeHandlerMethodFilter(ICommandFilter filter,
            CommandHandlingContext preContext, Func<CommandHandledContext> continuation)
        {
            filter.OnCommandHandling(preContext);

            if(!preContext.WillExecute) {
                return new CommandHandledContext();
            }

            bool wasError = false;
            CommandHandledContext postContext = null;
            try {
                postContext = continuation();
            }
            catch(ThreadAbortException) {
                postContext = new CommandHandledContext();
                filter.OnCommandHandled(postContext);
                throw;
            }
            catch(Exception ex) {
                wasError = true;
                postContext = new CommandHandledContext();
                filter.OnCommandHandled(postContext);
            }
            if(!wasError) {
                filter.OnCommandHandled(postContext);
            }
            return postContext;
        }
    }
}
