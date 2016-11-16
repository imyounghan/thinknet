using System;
using System.Reflection;
using ThinkNet.Contracts;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Domain.EventSourcing
{
    public class CommandResultRepliedInnerHandler : IProxyHandler, IHandler
    {
        private readonly ICommandResultNotification _notification;

        public CommandResultRepliedInnerHandler(ICommandResultNotification notification)
        {
            this._notification = notification;
        }

        public MethodInfo Method
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IHandler ReflectedHandler
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //public Type ContractType { get { return typeof(IHandler); } }

        //public Type TargetType { get { return typeof(CommandResultRepliedInnerHandler); } }

        public IHandler GetTargetHandler()
        {
            return this;
        }

        public void Handle(params object[] args)
        {
            this.Handle(args[0] as CommandResultReplied);
        }

        public void Handle(CommandResultReplied reply)
        {
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
