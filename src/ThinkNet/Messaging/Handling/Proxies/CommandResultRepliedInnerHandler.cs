using System.Reflection;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Proxies
{
    /// <summary>
    /// 命令结果的内部处理器
    /// </summary>
    public class CommandResultRepliedInnerHandler : IHandlerProxy
    {
        private readonly ICommandResultNotification _notification;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        /// <param name="notification"></param>
        public CommandResultRepliedInnerHandler(ICommandResultNotification notification)
        {
            this._notification = notification;
        }

        /// <summary>
        /// 反射方法
        /// </summary>
        public MethodInfo ReflectedMethod
        {
            get
            {
                return typeof(CommandResultRepliedInnerHandler).GetMethod("Handle");
            }
        }
        /// <summary>
        /// 处理器实例
        /// </summary>
        public object HandlerInstance { get { return this; } }

        /// <summary>
        /// 处理数据
        /// </summary>
        public void Handle(params object[] args)
        {
            var reply = args[0] as CommandResultReplied;

            switch (reply.CommandReturnType) {
                case CommandReturnType.CommandExecuted:
                    _notification.NotifyCommandHandled(reply);
                    break;
                case CommandReturnType.DomainEventHandled:
                    _notification.NotifyEventHandled(reply);
                    break;
            }
        }
    }
}
