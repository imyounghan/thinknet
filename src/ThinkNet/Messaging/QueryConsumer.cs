

namespace ThinkNet.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging.Handling;

    public class QueryConsumer : Processor, IInitializer
    {
        protected readonly ILogger logger;
        private readonly Dictionary<Type, IHandler> _handlers;
        private readonly Dictionary<Type, Type> _queryToResultMap; 
        private readonly IMessageReceiver<Envelope<IQuery>> receiver;
        private readonly ISendReplyService sendReplyService;


        public QueryConsumer(IMessageReceiver<Envelope<IQuery>> receiver, 
            ISendReplyService sendReplyService, 
            ILoggerFactory loggerFactory)
        {
            this._handlers = new Dictionary<Type, IHandler>();
            this._queryToResultMap = new Dictionary<Type, Type>();
            this.logger = loggerFactory.GetDefault();
            this.receiver = receiver;
            this.sendReplyService = sendReplyService;
        }

        protected override void Dispose(bool disposing)
        {
        }

        private void OnMessageReceived(object sender, Envelope<IQuery> envelope)
        {
            var queryType = envelope.Body.GetType();

            IHandler handler;
            if(!this._handlers.TryGetValue(queryType, out handler)) {
                var errorMessage = string.Format("The type('{0}') of handler is not found.", queryType.FullName);
                throw new ApplicationException(errorMessage);
            }

            var handlerContext = new HandlerContext(envelope.Body, handler);
            handlerContext.InvocationContext["TraceInfo"] = envelope.Items["TraceInfo"];
            handlerContext.InvocationContext["QueryId"] = envelope.MessageId;

            IEnumerable<Filter> filters = FilterProviders.Providers.GetFilters(handlerContext);
            var filterInfo = new FilterInfo(filters);

            try {
                ActionExecutedContext postContext = this.InvokeHandlerMethodWithFilters(
                    handlerContext,
                    filterInfo.ActionFilters,
                    envelope);
                if(!postContext.ExceptionHandled) {
                    this.InvokeActionResult(handlerContext, postContext.ReturnValue ?? postContext.Exception);
                }
                else {
                    this.InvokeActionResult(handlerContext, postContext.ReturnValue);
                }
            }
            catch(Exception ex) {
                ExceptionContext exceptionContext = InvokeExceptionFilters(
                    handlerContext,
                    filterInfo.ExceptionFilters,
                    ex);
                if(!exceptionContext.ExceptionHandled) {
                    this.InvokeActionResult(handlerContext, exceptionContext.ReturnValue ?? exceptionContext.Exception);
                }
                else {
                    this.InvokeActionResult(handlerContext, exceptionContext.ReturnValue);
                }
            }
        }

        private void InvokeActionResult(HandlerContext context, object result)
        {
            var traceInfo = (TraceInfo)context.InvocationContext["TraceInfo"];

            var ex = result as Exception;

            //var resultType = _queryToResultMap[context.Message.GetType()];
            //var queryResultType = typeof(QueryResult<>).MakeGenericType(resultType);
            QueryResult queryResult;

            // 如果出现了系统异常则将异常转换成返回结果发送
            if(ex != null)
            {
                queryResult = new QueryResult(traceInfo.Id, ExecutionStatus.Failed, ex.Message);
                this.sendReplyService.SendReply(queryResult, traceInfo.Address);
                return;
            }

            if (result == null || result == DBNull.Value)
            {
                queryResult = new QueryResult(traceInfo.Id, ExecutionStatus.Nothing);
                this.sendReplyService.SendReply(queryResult, traceInfo.Address);
                return;
            }

            queryResult = new QueryResult(traceInfo.Id) { Data = result };
            this.sendReplyService.SendReply(queryResult, traceInfo.Address);
        }

        private static ExceptionContext InvokeExceptionFilters(
           HandlerContext queryHandlerContext,
           IList<IExceptionFilter> filters,
           Exception exception)
        {
            var context = new ExceptionContext(queryHandlerContext, exception);
            foreach(IExceptionFilter filter in filters.Reverse()) {
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

            if(!preContext.WillExecute) {
                return new ActionExecutedContext(preContext, true, null) { ReturnValue = preContext.ReturnValue };
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

        private ActionExecutedContext InvokeHandlerMethodWithFilters(
            HandlerContext handlerContext,
            IList<IActionFilter> filters,
            Envelope<IQuery> envelope)
        {
            var preContext = new ActionExecutingContext(handlerContext);

            Func<ActionExecutedContext> continuation = () =>
                {
                    return new ActionExecutedContext(handlerContext, false, null)
                               {
                                   ReturnValue =
                                       this.Execute(
                                           handlerContext.Handler,
                                           envelope)
                               };
                };

            return
                filters.Reverse()
                    .Aggregate(
                        continuation,
                        (next, filter) => () => InvokeHandlerMethodFilter(filter, preContext, next))
                    .Invoke();
        }

        private object Execute(IHandler handler, Envelope<IQuery> envelope)
        {
            return ((dynamic)handler).Handle((dynamic)envelope.Body);
        }

        /// <summary>
        ///     启动进程
        /// </summary>
        protected override void Start()
        {
            this.receiver.MessageReceived += this.OnMessageReceived;
            this.receiver.Start();

            Console.WriteLine("Query Consumer Started!");
        }

        /// <summary>
        ///     停止进程
        /// </summary>
        protected override void Stop()
        {
            this.receiver.MessageReceived -= this.OnMessageReceived;
            this.receiver.Stop();

            Console.WriteLine("Query Consumer Stopped!");
        }

        #region IInitializer 成员

        public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            var filteredTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(FilterType)
                .SelectMany(type => type.GetInterfaces())
                .Where(FilterInterfaceType);

            foreach(var type in filteredTypes)
            {
                var arguments = type.GetGenericArguments();

                var queryType = arguments.First();
                if (this._handlers.ContainsKey(queryType))
                {
                    string errorMessage = string.Format(
                        "There are have duplicate IQueryHandler interface type for {0}.",
                        queryType.FullName);
                    throw new SystemException(errorMessage);
                }

                List<IHandler> queryHandlers = container.ResolveAll(type).OfType<IHandler>().ToList();
                if (queryHandlers.Count > 1)
                {
                    var errorMessage = string.Format(
                        "Found more than one handler for '{0}' with IQueryHandler.",
                        queryType.FullName);
                    throw new SystemException(errorMessage);
                }

                this._handlers[queryType] = queryHandlers.First();
                this._queryToResultMap[queryType] = arguments.Last();
            }

            Type baseType = typeof(IQuery);

            Type[] queryTypes =
                assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                    .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                    .ToArray();

            foreach (Type queryType in queryTypes)
            {
                if (!this._handlers.ContainsKey(queryType))
                {
                    var errorMessage = string.Format("The type('{0}') of handler is not found.", queryType.FullName);
                    throw new SystemException(errorMessage);
                }
            }
        }

        private static bool FilterInterfaceType(Type type)
        {
            if(!type.IsGenericType)
                return false;


            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IQueryHandler<,>);
        }

        private static bool FilterType(Type type)
        {
            if(!type.IsClass || type.IsAbstract)
                return false;

            return type.GetInterfaces().Any(FilterInterfaceType);
        }
        #endregion
    }
}
