
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
        class CommandDescriptor
        {
            public CommandDescriptor(string commandId, Command command, TraceInfo traceInfo)
            {
                this.Command = command;
                this.CommandType = command.GetType();
                this.CommandId = commandId;
                this.TraceInfo = traceInfo;
            }

            public Command Command { get; set; }

            public Type CommandType { get; set; }

            public IHandler CommandHandler { get; set; }

            public Type CommandHandlerType { get; set; }

            public string CommandId { get; set; }

            public TraceInfo TraceInfo { get; set; }

            public void Execute(Func<CommandDescriptor, ICommandContext> commandContextFactory)
            {
                if (this.CommandHandler is ICommandHandler)
                {
                    var context = commandContextFactory.Invoke(this);
                    ((dynamic)this.CommandHandler).Handle((dynamic)context, (dynamic)this.Command);
                    context.ActAs<IUnitOfWork>().Commit();
                }
                else if (this.CommandHandler is IEnvelopedHandler)
                {
                    var envelopedCommandType = typeof(Envelope<>).MakeGenericType(this.CommandType);
                    var envelopedCommand = Activator.CreateInstance(
                        envelopedCommandType,
                        new object[] { this.Command, this.CommandId });
                    ((dynamic)this.CommandHandler).Handle((dynamic)envelopedCommand);
                }
                else
                {
                    ((dynamic)this.CommandHandler).Handle((dynamic)this.Command);
                }
            }
        }


        private readonly Dictionary<Type, IHandler> _commandHandlers;
        private readonly IMessageBus<PublishableException> exceptionBus;
        private readonly ISendReplyService sendReplyService;

        private readonly IEventStore eventStore;
        private readonly ICache cache;
        private readonly ISnapshotStore snapshotStore;
        private readonly IEventBus eventBus;

        private readonly IRepository repository;


        public CommandConsumer(IMessageBus<PublishableException> exceptionBus,
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
        }

        protected override void OnMessageReceived(object sender, Envelope<Command> envelope)
        {
            var traceInfo = (TraceInfo)envelope.Items["TraceInfo"];
            var commandDescriptor = new CommandDescriptor(envelope.MessageId, envelope.Body, traceInfo);

            this.ProcessingCommand(commandDescriptor);
        }

        private void ProcessingCommand(CommandDescriptor commandDescriptor)
        {
            IHandler handler;
            if (!this._commandHandlers.TryGetValue(commandDescriptor.CommandType, out handler))
            {
                throw new HandlerNotFoundException(commandDescriptor.CommandType);
            }
            commandDescriptor.CommandHandler = handler;
            commandDescriptor.CommandHandlerType = handler.GetType();

            var handlerContext = new HandlerContext(commandDescriptor.Command, handler);
            handlerContext.InvocationContext["TraceInfo"] = commandDescriptor.TraceInfo;
            handlerContext.InvocationContext["Id"] = commandDescriptor.CommandId;


            var filters = FilterProviders.Providers.GetFilters(handlerContext);
            var filterInfo = new FilterInfo(filters);

            try
            {
                InvokeHandlerMethodWithFilters(handlerContext, filterInfo.ActionFilters, commandDescriptor);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                InvokeExceptionFilters(handlerContext, filterInfo.ExceptionFilters, ex);
            }
        }

        void InvokeHandlerMethodWithFilters(HandlerContext handlerContext, IList<IActionFilter> filters, CommandDescriptor commandDescriptor)
        {
            var preContext = new ActionExecutingContext(handlerContext);

            Func<ActionExecutedContext> continuation = () => {
                this.TryInvokeHandlerMethod(commandDescriptor);
                return new ActionExecutedContext(handlerContext, false, null);
            };

            filters.Reverse().Aggregate(continuation,
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


        void TryInvokeHandlerMethod(CommandDescriptor commandDescriptor)
        {
            try {
                TryMultipleInvokeHandlerMethod(commandDescriptor);
            }
            catch(Exception ex) {
                var commandResult = new CommandResult {
                    ProcessId = commandDescriptor.TraceInfo.ProcessId,
                    ErrorMessage = ex.Message,
                    ErrorCode = ex.HResult.ToString(),
                    ErrorData = ex.Data,
                    ReplyTime = DateTime.UtcNow
                };
                sendReplyService.SendReply(commandResult, commandDescriptor.TraceInfo.ReplyAddress);
                throw ex;
            }

            if (commandDescriptor.CommandHandler is ICommandHandler)
            {
                var commandResult = new CommandResult {
                    ProcessId = commandDescriptor.TraceInfo.ProcessId,
                    ReplyTime = DateTime.UtcNow
                };
                sendReplyService.SendReply(commandResult, commandDescriptor.TraceInfo.ReplyAddress);
            }
        }

        private void TryMultipleInvokeHandlerMethod(
            CommandDescriptor commandDescriptor,
            int retryTimes = 5,
            int retryInterval = 1000)
        {
            //try {
            //    commandDescriptor.Execute(this.CreateCommandContext);
            //}
            //catch(PublishableException ex) {
            //    exceptionBus.Send(ex);
            //    throw ex;
            //}
            //catch(Exception ex) {
            //    throw ex;
            //}

            int count = 0;
            while (count++ < retryTimes)
            {
                try
                {
                    commandDescriptor.Execute(this.CreateCommandContext);
                    break;
                }
                catch (PublishableException ex)
                {
                    if(logger.IsErrorEnabled) {
                        logger.Error(ex, "PublishableException raised when handling '{0}' on '{1}', Error will be send to bus.",
                            commandDescriptor.Command,
                            commandDescriptor.CommandHandlerType.FullName);
                    }

                    this.exceptionBus.Send(ex);
                    throw ex;
                }
                catch (Exception ex)
                {
                    if (count == retryTimes)
                    {
                        if (logger.IsErrorEnabled)
                        {
                            logger.Error(
                                ex,
                                "Exception raised when handling '{0}' on '{1}'.",
                                commandDescriptor.Command,
                                commandDescriptor.CommandHandlerType.FullName);
                        }
                        throw ex;
                    }
                    if (logger.IsWarnEnabled)
                    {
                        logger.Warn(
                            ex,
                            "An exception happened while handling '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                            commandDescriptor.Command,
                            commandDescriptor.CommandHandlerType.FullName,
                            count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }

            if (logger.IsDebugEnabled)
            {
                logger.DebugFormat(
                    "Handle '{0}' on '{1}' successfully.",
                    commandDescriptor.Command,
                    commandDescriptor.CommandHandlerType.FullName);
            }
        }

        ICommandContext CreateCommandContext(CommandDescriptor commandDescriptor)
        {
            return new CommandContext(
                this.eventBus,
                this.eventStore,
                this.snapshotStore,
                this.repository,
                this.cache,
                this.logger);
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
                    throw new SystemException(string.Format("Found more than one command handler for '{0}' with ICommandHandler<>.", commandType.FullName));
            }

            var handlers =
                    container.ResolveAll(typeof(IMessageHandler<>).MakeGenericType(commandType))
                        .OfType<IHandler>()
                        .ToList();
            switch(handlers.Count) {
                case 0:
                    throw new SystemException(string.Format("Command handler not found for '{0}'.", commandType.FullName));
                case 1:
                    _commandHandlers[commandType] = handlers.First();
                    break;
                default:
                    throw new SystemException(string.Format("Found more than one command handler for '{0}' with IMessageHandler<>.", commandType.FullName));
            }
        }

        //#region IInitializer 成员

        //public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        //{
        //    var commandTypes =
        //        assemblies.SelectMany(assembly => assembly.GetExportedTypes())
        //            .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableFrom(typeof(ICommand)))
        //            .ToArray();
            
        //    foreach(var commandType in commandTypes)
        //    {
        //        var commandHandlers =
        //            container.ResolveAll(typeof(ICommandHandler<>).MakeGenericType(commandType))
        //                .OfType<ICommandHandler>()
        //                .ToList();
        //        switch(commandHandlers.Count)
        //        {
        //            case 0:
        //                break;
        //            case 1:
        //                _commandHandlers[commandType] = commandHandlers.First();
        //                break;
        //            default:
        //                throw new SystemException(string.Format("Found more than one command handler for '{0}' with ICommandHandler<>.", commandType.FullName));
        //        }

        //        var envelopedCommandHandlers =
        //            container.ResolveAll(typeof(IEnvelopedMessageHandler<>).MakeGenericType(commandType))
        //                .OfType<IEnvelopeHandler>()
        //                .ToList();
        //        switch(envelopedCommandHandlers.Count) {
        //            case 0:
        //                break;
        //            case 1:
        //                _envelopeHandlers[commandType] = envelopedCommandHandlers.First();
        //                break;
        //            default:
        //                throw new SystemException(string.Format("Found more than one command handler for '{0}' with IEnvelopedMessageHandler<>.", commandType.FullName));
        //        }

        //        var handlers =
        //            container.ResolveAll(typeof(IMessageHandler<>).MakeGenericType(commandType))
        //                .OfType<IHandler>()
        //                .ToList();
        //        switch(handlers.Count) {
        //            case 0:
        //                throw new SystemException(string.Format("Command handler not found for '{0}'.", commandType.FullName));
        //            case 1:
        //                _handlers[commandType] = handlers.First();
        //                break;
        //            default:
        //                throw new SystemException(string.Format("Found more than one command handler for '{0}' with IMessageHandler<>.", commandType.FullName));
        //        }
        //    }
        //}

        //#endregion
    }
}
