
namespace ThinkNet.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging.Handling;
    using ThinkNet.Seeds;

    public class CommandConsumer : MessageConsumer<Command>, IInitializer
    {
        private readonly Dictionary<Type, IHandler> _commandHandlers;
        private readonly IMessageBus<IPublishableException> exceptionBus;
        private readonly ISendReplyService sendReplyService;

        private readonly IEventStore eventStore;
        private readonly ICache cache;
        private readonly ISnapshotStore snapshotStore;
        private readonly IEventBus eventBus;

        private readonly IRepository repository;


        public CommandConsumer(IMessageBus<IPublishableException> exceptionBus,
            ISendReplyService sendReplyService,
            IEventStore eventStore,
            ISnapshotStore snapshotStore,
            IRepository repository,
            ICache cache,
            IEventBus eventBus,
            ILoggerFactory loggerFactory,
            IMessageReceiver<Envelope<Command>> commandReceiver)
            : base(commandReceiver, loggerFactory.GetDefault(), "Command")
        {
            this.exceptionBus = exceptionBus;
            this.sendReplyService = sendReplyService;

            this.eventStore = eventStore;
            this.snapshotStore = snapshotStore;
            this.repository = repository;
            this.cache = cache;
            this.eventBus = eventBus;

            this._commandHandlers = new Dictionary<Type, IHandler>();

            this.CheckMode = CheckHandlerMode.OnlyOne;
        }

        protected override void OnMessageReceived(object sender, Envelope<Command> envelope)
        {
            var commandType = envelope.Body.GetType();

            IHandler handler;
            if (!this._commandHandlers.TryGetValue(commandType, out handler))
            {
                handler = this.GetHandlers(commandType).FirstOrDefault();
            }

            var handlerContext = new HandlerContext(envelope.Body, handler);
            handlerContext.InvocationContext["TraceInfo"] = envelope.Items["TraceInfo"];
            handlerContext.InvocationContext["CommandId"] = envelope.MessageId;


            var filters = FilterProviders.Providers.GetFilters(handlerContext);
            var filterInfo = new FilterInfo(filters);

            try
            {
                var postContext = InvokeHandlerMethodWithFilters(handlerContext, filterInfo.ActionFilters, envelope);
                if(!postContext.ExceptionHandled) {
                    this.InvokeActionResult(handlerContext, postContext.ReturnValue ?? postContext.Exception);
                }
                else {
                    this.InvokeActionResult(handlerContext, postContext.ReturnValue);
                }
            }
            //catch (ThreadAbortException)
            //{
            //    throw;
            //}
            catch (Exception ex)
            {
                var exceptionContext = InvokeExceptionFilters(handlerContext, filterInfo.ExceptionFilters, ex);
                if (!exceptionContext.ExceptionHandled)
                {
                    this.InvokeActionResult(handlerContext, exceptionContext.ReturnValue ?? exceptionContext.Exception);
                }
                else
                {
                    this.InvokeActionResult(handlerContext, exceptionContext.ReturnValue);
                }
            }
        }

        ActionExecutedContext InvokeHandlerMethodWithFilters(HandlerContext handlerContext, IList<IActionFilter> filters, Envelope<Command> envelope)
        {
            var preContext = new ActionExecutingContext(handlerContext);

            Func<ActionExecutedContext> continuation = () => {
                this.ProcessMessage(envelope);
                return new ActionExecutedContext(handlerContext, false, null);
            };

            return filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokeHandlerMethodFilter(filter, preContext, next))
                .Invoke();
        }

        static ActionExecutedContext InvokeHandlerMethodFilter(IActionFilter filter, ActionExecutingContext preContext, Func<ActionExecutedContext> continuation)
        {
            filter.OnActionExecuting(preContext);

            if(!preContext.WillExecute) {
                return new ActionExecutedContext(preContext, true, null) {
                    ReturnValue = preContext.ReturnValue
                };
            }

            bool wasError = false;
            ActionExecutedContext postContext = null;

            try {
                postContext = continuation();
            }
            //catch(ThreadAbortException) {
            //    postContext = new ActionExecutedContext(preContext, false /* canceled */, null /* exception */);
            //    filter.OnActionExecuted(postContext);
            //    throw;
            //}
            catch(Exception ex) {
                wasError = true;
                postContext = new ActionExecutedContext(preContext, false, ex);
                filter.OnActionExecuted(postContext);
                if(!postContext.ExceptionHandled) {
                    throw;
                }
            }
            if(!wasError) {
                filter.OnActionExecuted(postContext);
            }
            return postContext;
        }

        static ExceptionContext InvokeExceptionFilters(HandlerContext commandHandlerContext, IList<IExceptionFilter> filters, Exception exception)
        {
            var context = new ExceptionContext(commandHandlerContext, exception);
            foreach(var filter in filters.Reverse()) {
                filter.OnException(context);
            }

            return context;
        }

        void InvokeActionResult(HandlerContext context, object result)
        {
            var traceInfo = (TraceInfo)context.InvocationContext["TraceInfo"];

            var commandResult = result as CommandResult;
            //如果自己设置了返回结果直接发送
            if (commandResult != null)
            {
                sendReplyService.SendReply(commandResult, traceInfo.ReplyAddress);
                return;
            }

            var publishableException = result as IPublishableException;
            //如果出现了可发布异常则将异常转换成返回结果发送
            if(publishableException != null) {
                commandResult = new CommandResult {
                    ProcessId = traceInfo.ProcessId,
                    ErrorMessage = publishableException.Message,
                    ErrorCode = publishableException.ErrorCode,
                    ErrorData = publishableException.Data,
                    ReplyTime = DateTime.UtcNow
                };
                exceptionBus.Send(publishableException);
                sendReplyService.SendReply(commandResult, traceInfo.ReplyAddress);
                return;
            }

            var ex = result as Exception;
            //如果出现了系统异常则将异常转换成返回结果发送
            if (ex != null)
            {
                commandResult = new CommandResult {
                    ProcessId = traceInfo.ProcessId,
                    ErrorMessage = ex.Message,
                    ErrorCode = "-1",
                    ErrorData = ex.Data,
                    ReplyTime = DateTime.UtcNow
                };
                sendReplyService.SendReply(commandResult, traceInfo.ReplyAddress);
                return;
            }

            //如果是非ICommandHandler处理器则发送成功结果
            if(!(context.Handler is ICommandHandler)) {
                commandResult = new CommandResult {
                    ProcessId = traceInfo.ProcessId,
                    ReplyTime = DateTime.UtcNow
                };
                sendReplyService.SendReply(commandResult, traceInfo.ReplyAddress);
            }

            //剩下的则交由事件处理器来发送
        }

        private void InvokeHandler(IHandler commandHandler, Envelope<Command> envelope)
        {
            var context = new CommandContext(
                    this.eventBus,
                    this.eventStore,
                    this.snapshotStore,
                    this.repository,
                    this.cache,
                    this.logger);
            context.CommandId = envelope.MessageId;
            context.Command = envelope.Body;
            context.TraceInfo = (TraceInfo)envelope.Items["TraceInfo"];

            ((dynamic)commandHandler).Handle((dynamic)context, (dynamic)envelope.Body);
            context.Commit();
        }

        private void ProcessMessage(Envelope<Command> envelope)
        {
            var commandType = envelope.Body.GetType();

            if(_commandHandlers.ContainsKey(commandType))
            {
                TryMultipleInvoke(InvokeHandler, _commandHandlers[commandType], envelope);
                return;
            }

            this.ProcessMessage(envelope, commandType);
        }
        
        protected override void Initialize(IObjectContainer container, Type commandType)
        {
            var commandHandlers =
                    container.ResolveAll(typeof(ICommandHandler<>).MakeGenericType(commandType))
                        .OfType<ICommandHandler>()
                        .ToList();
            switch(commandHandlers.Count) {
                case 0:
                    break;
                case 1:
                    _commandHandlers[commandType] = commandHandlers.First();
                    return;
                default:
                    throw new SystemException(string.Format("Found more than one handler for '{0}' with ICommandHandler<>.", commandType.FullName));
            }

            base.Initialize(container, commandType);
        }
    }
}
