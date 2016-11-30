using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkLib;
using ThinkLib.Interception;
using ThinkLib.Interception.Pipeline;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 命令处理的代理器
    /// </summary>
    public class CommandHandlerAgent : HandlerAgent
    {
        private readonly Func<CommandContext> _commandContextFactory;
        private readonly IHandler _targetHandler;
        private readonly Type _contractType;
        private readonly IInterceptorProvider _interceptorProvider;
        private readonly IMessageBus _messageBus;
        private readonly IMessageHandlerRecordStore _handlerStore;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public CommandHandlerAgent(Type commandHandlerInterfaceType,
            IHandler handler,
            Func<CommandContext> commandContextFactory,
            IMessageBus messageBus,
            IMessageHandlerRecordStore handlerStore,
            IInterceptorProvider interceptorProvider)
        {
            this._commandContextFactory = commandContextFactory;
            this._targetHandler = handler;
            this._contractType = commandHandlerInterfaceType;
            this._messageBus = messageBus;
            this._handlerStore = handlerStore;
            this._interceptorProvider = interceptorProvider;
        }


        protected override void TryHandle(object[] args)
        {
            if(args.Length == 1) {
                ((dynamic)_targetHandler).Handle((dynamic)args[0]);
                return;
            }

            var context = args[0] as CommandContext;
            context.NotNull("context");
            var command = args[1] as Command;
            command.NotNull("command");

            ((dynamic)_targetHandler).Handle((dynamic)context, (dynamic)command);
            context.Commit(command.Id);
        }

        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }

        public override void Handle(object[] args)
        {
            var command = args.Last() as Command;
            var commandType = command.GetType();
            var commandHandlerType = _targetHandler.GetType();

            if(_handlerStore.HandlerIsExecuted(command.Id, commandType, commandHandlerType)) {
                var errorMessage = string.Format("The command has been handled. CommandHandlerType:{0}, CommandType:{1}, CommandId:{2}.",
                    commandHandlerType.FullName, commandType.FullName, command.Id);
                _messageBus.Publish(new CommandResult(command.Id, errorMessage, "-1"));
                if(LogManager.Default.IsWarnEnabled) {
                    LogManager.Default.Warn(errorMessage);
                }
                return;
            }

            if(_commandContextFactory != null) {
                args = new object[] { _commandContextFactory.Invoke(), args[0] };
            }

            try {
                TryHandleWithFilter(args);
                _messageBus.Publish(new CommandResult(command.Id));
            }
            catch(Exception ex) {
                _messageBus.Publish(new CommandResult(command.Id, ex));
                throw ex;
            }

            _handlerStore.AddHandlerInfo(command.Id, commandType, commandHandlerType);
        }

        private void TryHandleWithFilter(object[] args)
        {
            var pipeline = this.GetInterceptorPipeline();
            if(pipeline == null || pipeline.Count == 0) {
                base.Handle(args);
                return;
            }

            var methodInfo = this.GetReflectedMethodInfo();
            var input = new MethodInvocation(_targetHandler, methodInfo, args);
            var methodReturn = pipeline.Invoke(input, delegate {
                try {
                    base.Handle(args);
                    return new MethodReturn(input, null, args);
                }
                catch(Exception ex) {
                    return new MethodReturn(input, ex);
                }
            });

            if(methodReturn.Exception != null)
                throw methodReturn.Exception;
        }


        private readonly static ConcurrentDictionary<Type, MethodInfo> HandleMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

        private MethodInfo GetReflectedMethodInfo()
        {
            return HandleMethodCache.GetOrAdd(_contractType, delegate (Type type) {
                var interfaceMap = _targetHandler.GetType().GetInterfaceMap(type);
                return interfaceMap.TargetMethods.FirstOrDefault();
            });
        }

        private InterceptorPipeline GetInterceptorPipeline()
        {
            if(!ConfigurationSetting.Current.EnableCommandFilter) {
                return null;
            }

            var method = this.GetReflectedMethodInfo();
            return InterceptorPipelineManager.Instance.CreatePipeline(method, 
                _interceptorProvider.GetInterceptors);
        }
    }

    //public class CommandHandlerAgent<TCommand> : HandlerAgent
    //    where TCommand : Command
    //{
    //    private readonly Func<CommandContext> _commandContextFactory;
    //    private readonly ICommandHandler<TCommand> _targetHandler;
    //    private readonly Type _contractType;
    //    /// <summary>
    //    /// Parameterized constructor.
    //    /// </summary>
    //    public CommandHandlerAgent(ICommandHandler<TCommand> handler,
    //        Func<CommandContext> commandContextFactory,
    //        IInterceptorProvider interceptorProvider,
    //        IEnumerable<IInterceptor> firstInInterceptors,
    //        IEnumerable<IInterceptor> lastInInterceptors)
    //        : base(interceptorProvider, firstInInterceptors, lastInInterceptors)
    //    {
    //        this._commandContextFactory = commandContextFactory;
    //        this._targetHandler = handler;
    //        this._contractType = typeof(ICommandHandler<TCommand>);
    //    }

    //    protected override void TryHandle(object[] args)
    //    {
    //        var context = args[0] as CommandContext;
    //        context.NotNull("context");
    //        var command = args[1] as TCommand;
    //        command.NotNull("command");

    //        _targetHandler.Handle(context, command);
    //        context.Commit(command.Id);
    //    }

    //    protected override Type GetHandlerInterfaceType()
    //    {
    //        return this._contractType;
    //    }

    //    public override object GetInnerHandler()
    //    {
    //        return this._targetHandler;
    //    }


    //    public override void Handle(object[] args)
    //    {
    //        args = new object[] { _commandContextFactory.Invoke(), args[0] };

    //        base.Handle(args);
    //    }
    //    //void IHandlerAgent.Handle(object[] args)
    //    //{
    //    //    args = new object[] { commandContextFactory.Invoke(), args[0] };
    //    //    this.Handle(args);
    //    //}
    //}
}
