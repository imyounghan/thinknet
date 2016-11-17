using System;
using ThinkNet.Common.Interception;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Proxies
{
    public class NotifyCommandResultInterceptor : IInterceptor
    {
        private readonly IMessageBus _bus;

        public NotifyCommandResultInterceptor(IMessageBus bus)
        {
            this._bus = bus;
        }

        private void SendCommandResult(IMethodInvocation input, Exception ex)
        {
            CommandReturnType? commandReturnType = null;
            string commandId = null;

            var parameter = input.Arguments.GetParameterInfo(input.Arguments.Count - 1);
            var argument = input.Arguments[parameter.Name];

            var eventStream = argument as EventStream;
            if(eventStream != null) {
                if(ex == null) {
                    _bus.Publish(eventStream.Events);
                }
                else if(ex is DomainEventAsPendingException) {
                    _bus.Publish(eventStream);
                    return;
                }

                commandReturnType = CommandReturnType.DomainEventHandled;
                commandId = eventStream.CorrelationId;
            }

            var command = argument as ICommand;
            if(command != null) {
                commandReturnType = CommandReturnType.CommandExecuted;
                commandId = command.UniqueId;
            }                        

            if(!commandReturnType.HasValue || string.IsNullOrEmpty(commandId))
                return;

            var commandResult = new CommandResultReplied(commandId, commandReturnType.Value, ex);
            _bus.Publish(commandResult);
        }

        public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptorDelegate getNext)
        {
            var methodReturn = getNext().Invoke(input, getNext);

            SendCommandResult(input, methodReturn.Exception);

            return methodReturn;
        }
    }
}
