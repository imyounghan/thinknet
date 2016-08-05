﻿using System;
using System.Collections;
using System.Collections.Generic;
using ThinkNet.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class CommandReplyProcessor : Processor, IProcessor
    {
        private readonly ICommandNotification _notification;
        private readonly IEnvelopeDelivery _envelopeDelivery;

        public CommandReplyProcessor(ICommandResultManager commandResultManager,
            IEnvelopeDelivery envelopeDelivery)
        {
            this._envelopeDelivery = envelopeDelivery;
            this._notification = commandResultManager as ICommandNotification;
            base.BuildWorker(EnvelopeBuffer<CommandReply>.Instance.Dequeue, Process);            
        }

        void Process(Envelope<CommandReply> item)
        {
            var reply = item.Body;

            switch (reply.CommandResultType) {
                case CommandResultType.CommandExecuted:
                    //_notification.NotifyHandled(new CommandResult(reply.Status, reply.CommandId, reply.ExceptionTypeName, reply.ErrorMessage, reply.ErrorData));
                    _notification.NotifyHandled(reply.CommandId, reply.GetInnerException());
                    break;
                case CommandResultType.DomainEventHandled:
                    //_notification.NotifyCommandCompleted(new CommandResult(reply.Status, reply.CommandId, reply.ExceptionTypeName, reply.ErrorMessage, reply.ErrorData));
                    _notification.NotifyCompleted(reply.CommandId, reply.GetInnerException());
                    break;
            }

            _envelopeDelivery.Post(item);
        }
        
    }
}
