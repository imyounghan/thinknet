
namespace ThinkNet.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging.Handling;
    using ThinkNet.Seeds;

    /// <summary>
    /// The command consumer.
    /// </summary>
    public class CommandConsumer : MessageConsumer<ICommand>, IInitializer
    {
        #region Fields

        private readonly Dictionary<Type, IHandler> _commandHandlers;
        private readonly ICache cache;
        private readonly IEventBus eventBus;
        private readonly IEventStore eventStore;
        private readonly IMessageBus<IPublishableException> exceptionBus;
        private readonly IRepository repository;
        private readonly ISendReplyService sendReplyService;
        private readonly ISnapshotStore snapshotStore;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandConsumer"/> class.
        /// </summary>
        public CommandConsumer(
            IMessageBus<IPublishableException> exceptionBus, 
            ISendReplyService sendReplyService, 
            IEventStore eventStore, 
            ISnapshotStore snapshotStore, 
            IRepository repository, 
            ICache cache, 
            IEventBus eventBus, 
            ILoggerFactory loggerFactory,
            IMessageReceiver<Envelope<ICommand>> commandReceiver)
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

        #endregion

        #region Methods

        protected override void Initialize(IObjectContainer container, Type commandType)
        {
            List<ICommandHandler> commandHandlers =
                container.ResolveAll(typeof(ICommandHandler<>).MakeGenericType(commandType))
                    .OfType<ICommandHandler>()
                    .ToList();
            switch (commandHandlers.Count)
            {
                case 0:
                    break;
                case 1:
                    this._commandHandlers[commandType] = commandHandlers.First();
                    return;
                default:
                    throw new SystemException(
                        string.Format(
                            "Found more than one handler for '{0}' with ICommandHandler<>.", 
                            commandType.FullName));
            }

            base.Initialize(container, commandType);
        }

        protected override void OnMessageReceived(object sender, Envelope<ICommand> envelope)
        {
            Type commandType = envelope.Body.GetType();

            IHandler handler;
            if (!this._commandHandlers.TryGetValue(commandType, out handler))
            {
                handler = this.GetHandlers(commandType).FirstOrDefault();
            }

            var handlerContext = new HandlerContext(envelope.Body, handler);
            handlerContext.InvocationContext["TraceInfo"] = envelope.Items["TraceInfo"];
            handlerContext.InvocationContext["CommandId"] = envelope.MessageId;

            IEnumerable<Filter> filters = FilterProviders.Providers.GetFilters(handlerContext);
            var filterInfo = new FilterInfo(filters);

            try
            {
                ActionExecutedContext postContext = this.InvokeHandlerMethodWithFilters(
                    handlerContext, 
                    filterInfo.ActionFilters, 
                    envelope);
                if (!postContext.ExceptionHandled)
                {
                    this.InvokeActionResult(handlerContext, postContext.ReturnValue ?? postContext.Exception);
                }
                else
                {
                    this.InvokeActionResult(handlerContext, postContext.ReturnValue);
                }
            }
                
                // catch (ThreadAbortException)
                // {
                // throw;
                // }
            catch (Exception ex)
            {
                ExceptionContext exceptionContext = InvokeExceptionFilters(
                    handlerContext, 
                    filterInfo.ExceptionFilters, 
                    ex);
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

        private static ExceptionContext InvokeExceptionFilters(
            HandlerContext commandHandlerContext, 
            IList<IExceptionFilter> filters, 
            Exception exception)
        {
            var context = new ExceptionContext(commandHandlerContext, exception);
            foreach (IExceptionFilter filter in filters.Reverse())
            {
                filter.OnException(context);
            }

            return context;
        }

        private static ActionExecutedContext InvokeHandlerMethodFilter(
            IActionFilter filter, 
            ActionExecutingContext preContext, 
            Func<ActionExecutedContext> continuation)
        {
            filter.OnActionExecuting(preContext);

            if (!preContext.WillExecute)
            {
                return new ActionExecutedContext(preContext, true, null) { ReturnValue = preContext.ReturnValue };
            }

            bool wasError = false;
            ActionExecutedContext postContext = null;

            try
            {
                postContext = continuation();
            }
                
                // catch(ThreadAbortException) {
                // postContext = new ActionExecutedContext(preContext, false /* canceled */, null /* exception */);
                // filter.OnActionExecuted(postContext);
                // throw;
                // }
            catch (Exception ex)
            {
                wasError = true;
                postContext = new ActionExecutedContext(preContext, false, ex);
                filter.OnActionExecuted(postContext);
                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                filter.OnActionExecuted(postContext);
            }

            return postContext;
        }

        private void InvokeActionResult(HandlerContext context, object result)
        {
            var traceInfo = (TraceInfo)context.InvocationContext["TraceInfo"];

            var commandResult = result as CommandResult;

            // 如果自己设置了返回结果直接发送
            if (commandResult != null)
            {
                this.sendReplyService.SendReply(commandResult, traceInfo.Address);
                return;
            }

            var publishableException = result as IPublishableException;

            // 如果出现了可发布异常则将异常转换成返回结果发送
            if (publishableException != null)
            {
                commandResult = new CommandResult {
                    TraceId = traceInfo.Id,
                    ErrorMessage = publishableException.Message,
                    ErrorCode = publishableException.ErrorCode,
                    ErrorData = publishableException.Data, 
                    ReplyTime = DateTime.UtcNow
                };
                this.exceptionBus.Send(publishableException);
                this.sendReplyService.SendReply(commandResult, traceInfo.Address);
                return;
            }

            var ex = result as Exception;

            // 如果出现了系统异常则将异常转换成返回结果发送
            if (ex != null)
            {
                commandResult = new CommandResult {
                    TraceId = traceInfo.Id,
                    ErrorMessage = ex.Message,
                    ErrorCode = "-1",
                    ErrorData = ex.Data, 
                    ReplyTime = DateTime.UtcNow
                };
                this.sendReplyService.SendReply(commandResult, traceInfo.Address);
                return;
            }

            // 如果是非ICommandHandler处理器则发送成功结果
            if (!(context.Handler is ICommandHandler))
            {
                commandResult = new CommandResult { TraceId = traceInfo.Id, ReplyTime = DateTime.UtcNow };
                this.sendReplyService.SendReply(commandResult, traceInfo.Address);
            }

            // 剩下的则交由事件处理器来发送
        }

        private void InvokeHandler(IHandler commandHandler, Envelope<ICommand> envelope)
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

        private ActionExecutedContext InvokeHandlerMethodWithFilters(
            HandlerContext handlerContext, 
            IList<IActionFilter> filters,
            Envelope<ICommand> envelope)
        {
            var preContext = new ActionExecutingContext(handlerContext);

            Func<ActionExecutedContext> continuation = () =>
                {
                    this.ProcessMessage(envelope);
                    return new ActionExecutedContext(handlerContext, false, null);
                };

            return
                filters.Reverse()
                    .Aggregate(
                        continuation, 
                        (next, filter) => () => InvokeHandlerMethodFilter(filter, preContext, next))
                    .Invoke();
        }

        private void ProcessMessage(Envelope<ICommand> envelope)
        {
            Type commandType = envelope.Body.GetType();

            if (this._commandHandlers.ContainsKey(commandType))
            {
                this.TryMultipleInvoke(this.InvokeHandler, this._commandHandlers[commandType], envelope);
                return;
            }

            this.ProcessMessage(envelope, commandType);
        }

        #endregion
    }
}