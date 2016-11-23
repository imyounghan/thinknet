using System;
using System.Reflection;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 命令结果的内部处理器
    /// </summary>
    public class CommandResultInnerHandler : HandlerAgent
    {
        private readonly ICommandResultNotification _notification;
        private readonly Lazy<MethodInfo> _method;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        /// <param name="notification"></param>
        public CommandResultInnerHandler(ICommandResultNotification notification)
            : base(null)
        {
            this._notification = notification;
            //this._method = new Lazy<MethodInfo>(GetMethodInfo);
        }

        protected override void TryHandle(object[] args)
        {
            var reply = args[0] as CommandResult;

            switch(reply.CommandReturnType) {
                case CommandReturnType.CommandExecuted:
                    _notification.NotifyCommandHandled(reply);
                    break;
                case CommandReturnType.DomainEventHandled:
                    _notification.NotifyEventHandled(reply);
                    break;
            }
        }       

        ///// <summary>
        ///// 反射方法
        ///// </summary>
        //public override MethodInfo ReflectedMethod { get { return _method.Value; } }
        ///// <summary>
        ///// 处理器实例
        ///// </summary>
        //public override object HandlerInstance { get { return this; } }

        //protected override void TryMultipleHandle(object[] args)
        //{
        //    var reply = args[0] as CommandResult;

        //    switch(reply.CommandReturnType) {
        //        case CommandReturnType.CommandExecuted:
        //            _notification.NotifyCommandHandled(reply);
        //            break;
        //        case CommandReturnType.DomainEventHandled:
        //            _notification.NotifyEventHandled(reply);
        //            break;
        //    }
        //}
    }
}
