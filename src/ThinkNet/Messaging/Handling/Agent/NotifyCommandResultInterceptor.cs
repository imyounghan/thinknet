﻿using System;
using ThinkLib.Interception;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 通知命令结果的拦截器
    /// </summary>
    public class NotifyCommandResultInterceptor : IInterceptor
    {
        private readonly IMessageBus _bus;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
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
                commandId = command.Id;
            }

            if(!commandReturnType.HasValue || string.IsNullOrEmpty(commandId))
                return;

            var commandResult = new CommandResult(commandId, ex, null, commandReturnType.Value);
            _bus.Publish(commandResult);
        }

        /// <summary>
        /// 发送命令结果
        /// </summary>
        public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptorDelegate getNext)
        {
            if(input.InvocationContext.ContainsKey("CommandStatus")) {
                var commandResult = new CommandResult((string)input.InvocationContext["CommandId"], 
                    (CommandReturnType)input.InvocationContext["CommandReturnType"], 
                    (CommandStatus)input.InvocationContext["CommandStatus"]);
                _bus.Publish(commandResult);

                return input.CreateExceptionMethodReturn(new ThinkNetException(""));
            }

            var methodReturn = getNext().Invoke(input, getNext);

            SendCommandResult(input, methodReturn.Exception);

            return methodReturn;
        }
    }
}
