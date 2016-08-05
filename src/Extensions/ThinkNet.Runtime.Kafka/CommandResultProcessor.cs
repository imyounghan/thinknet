using System;
using System.Collections.Generic;
using ThinkNet.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class CommandResultProcessor : CommandResultManager, IProcessor, IInitializer
    {
        private readonly object lockObject;
        private readonly Worker worker;
        private bool started;

        public CommandResultProcessor(ICommandBus commandBus)
            : base(commandBus)
        {
            this.lockObject = new object();
            this.worker = WorkerFactory.Create(EnvelopeBuffer<CommandReply>.Instance.Dequeue, Process);
        }

        void Process(Envelope<CommandReply> item)
        {
            var reply = item.Body;

            switch (reply.CommandResultType) {
                case CommandResultType.CommandExecuted:
                    this.NotifyCommandExecuted(new CommandResult(reply.Status, reply.CommandId, reply.ExceptionTypeName, reply.ErrorMessage, reply.ErrorData));
                    break;
                case CommandResultType.DomainEventHandled:
                    this.NotifyCommandCompleted(new CommandResult(reply.Status, reply.CommandId, reply.ExceptionTypeName, reply.ErrorMessage, reply.ErrorData));
                    break;
            }
        }
               


        #region IProcessor 成员

        public void Start()
        {
            lock (this.lockObject) {
                if (!this.started) {
                    worker.Start();
                    this.started = true;
                }
            }
        }

        public void Stop()
        {
            lock (this.lockObject) {
                if (this.started) {
                    worker.Stop();
                    this.started = false;
                }
            }
        }

        #endregion

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            this.Start();
        }

        #endregion
    }
}
