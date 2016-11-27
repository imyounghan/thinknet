using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkLib;
using ThinkLib.Interception;
using ThinkLib.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Agent
{
    ///// <summary>
    ///// 命令处理的代理器
    ///// </summary>
    //public class CommandHandlerAgent : MessageHandlerAgent
    //{
    //    private readonly Func<CommandContext> commandContextFactory;
    //    /// <summary>
    //    /// Parameterized constructor.
    //    /// </summary>
    //    public CommandHandlerAgent(object handler, MethodInfo method, 
    //        InterceptorPipeline pipeline,
    //        Func<CommandContext> commandContextFactory)
    //        : base(handler, method, pipeline)
    //    {
    //        this.commandContextFactory = commandContextFactory;
    //    }

    //    /// <summary>
    //    /// 处理命令
    //    /// </summary>
    //    /// <param name="args"></param>
    //    public override void Handle(object[] args)
    //    {
    //        if (commandContextFactory != null) {
    //            args = new object[] { commandContextFactory.Invoke(), args[0] };
    //        }

    //        base.Handle(args);
    //    }

    //    protected override void TryHandle(object[] args)
    //    {
    //        base.TryHandle(args);

    //        if(args.Length == 2) {
    //            var commandContext = args[0] as CommandContext;
    //            var command = args[1] as Command;

    //            commandContext.Commit(command.Id);
    //        }
    //    }
    //}

    public class CommandHandlerAgent<TCommand> : HandlerAgent
        where TCommand : Command
    {
        private readonly Func<CommandContext> _commandContextFactory;
        private readonly ICommandHandler<TCommand> _targetHandler;
        private readonly Type _contractType;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public CommandHandlerAgent(ICommandHandler<TCommand> handler,
            Func<CommandContext> commandContextFactory,
            IInterceptorProvider interceptorProvider,
            IEnumerable<IInterceptor> firstInInterceptors,
            IEnumerable<IInterceptor> lastInInterceptors)
            : base(interceptorProvider, firstInInterceptors, lastInInterceptors)
        {
            this._commandContextFactory = commandContextFactory;
            this._targetHandler = handler;
            this._contractType = typeof(ICommandHandler<TCommand>);
        }

        protected override void TryHandle(object[] args)
        {
            var context = args[0] as CommandContext;
            context.NotNull("context");
            var command = args[0] as TCommand;
            command.NotNull("command");

            _targetHandler.Handle(context, command);
            context.Commit(command.Id);
        }

        protected override Type GetHandlerInterfaceType()
        {
            return this._contractType;
        }

        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }


        public override void Handle(object[] args)
        {
            args = new object[] { _commandContextFactory.Invoke(), args[0] };

            base.Handle(args);
        }
        //void IHandlerAgent.Handle(object[] args)
        //{
        //    args = new object[] { commandContextFactory.Invoke(), args[0] };
        //    this.Handle(args);
        //}
    }
}
