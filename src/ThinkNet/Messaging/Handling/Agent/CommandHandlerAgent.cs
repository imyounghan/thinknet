using System;
using System.Collections.Generic;
using ThinkLib;
using ThinkLib.Interception;

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
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public CommandHandlerAgent(Type commandHandlerInterfaceType,
            IHandler handler,
            Func<CommandContext> commandContextFactory,
            IInterceptorProvider interceptorProvider,
            IEnumerable<IInterceptor> firstInInterceptors,
            IEnumerable<IInterceptor> lastInInterceptors)
            : base(interceptorProvider, firstInInterceptors, lastInInterceptors)
        {
            this._commandContextFactory = commandContextFactory;
            this._targetHandler = handler;
            this._contractType = commandHandlerInterfaceType;
        }


        protected override void TryHandle(object[] args)
        {
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

        protected override Type GetHandlerInterfaceType()
        {
            return this._contractType;
        }

        public override void Handle(object[] args)
        {
            args = new object[] { _commandContextFactory.Invoke(), args[0] };

            base.Handle(args);
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
