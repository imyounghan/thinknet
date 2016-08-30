using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    public class CommandNotification : DisposableObject, ICommandNotification, IInitializer
    {
        private readonly BlockingCollection<CommandReply> _broker;
        private readonly IEnvelopeSender _sender;

        private CancellationTokenSource cancellationSource;

        public CommandNotification(IEnvelopeSender sender)
        { 
            this._sender = sender;
            this._broker = new BlockingCollection<CommandReply>();
        }

        public void NotifyCompleted(string messageId, Exception exception = null)
        {
            _broker.Add(new CommandReply(messageId, exception, CommandReturnType.DomainEventHandled));
        }

        public void NotifyHandled(string messageId, Exception exception = null)
        {
            _broker.Add(new CommandReply(messageId, exception, CommandReturnType.CommandExecuted));
        }

        public void NotifyUnchanged(string messageId)
        {
            _broker.Add(new CommandReply(messageId));
        }

        private Envelope Transform(CommandReply reply)
        {
            return new Envelope() {
                Body = reply,
                CorrelationId = reply.Id,
                RoutingKey = reply.CommandId,
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.cancellationSource != null) {
                using (this.cancellationSource) {
                    this.cancellationSource.Cancel();
                    this.cancellationSource = null;
                }
            }
        }

        #region IInitializer 成员
        private void Consume(object state)
        {
            var broker = state as BlockingCollection<CommandReply>;
            broker.NotNull("broker");

            while (!cancellationSource.Token.IsCancellationRequested) {
                var messages = broker.GetConsumingEnumerable();
                _sender.SendAsync(messages.Select(Transform)).Wait();
            }
        }

        public void Initialize(IEnumerable<Type> types)
        {
            if (this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();

                Task.Factory.StartNew(Consume, _broker,
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Current);
            }
        }

        #endregion
    }
}
