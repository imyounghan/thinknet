using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class DefaultMessageProcessor : IProcessor
    {
        private readonly IEnvelopeReceiver _receiver;

        private readonly Dictionary<string, IMessageExecutor> executorDict;
        private readonly object lockObject;

        public DefaultMessageProcessor(IEnvelopeReceiver receiver,
            ICommandNotification notification, 
            IHandlerProvider handlerProvider,
            IHandlerRecordStore handlerStore,
            IEventBus eventBus,
            IEventPublishedVersionStore eventPublishedVersionStore,
            ISerializer serializer)
        {
            this._receiver = receiver;

            this.executorDict = new Dictionary<string, IMessageExecutor>() {
                { "Command", new CommandExecutor(notification, handlerProvider) },
                { "Event", new EventExecutor(handlerStore, handlerProvider) },
                { "EventStream", new SynchronousExecutor(notification, handlerProvider, eventBus, eventPublishedVersionStore, serializer) }
            };
            this.lockObject = new object();
        }

        protected void AddExecutor(string kind, IMessageExecutor executor)
        {
            executorDict.Add(kind, executor);
        }

        private void OnEnvelopeReceived(object sender, Envelope envelope)
        {
            TimeSpan processTime;
            executorDict[envelope.Kind].Execute(envelope.Body, out processTime);
            envelope.ProcessTime = processTime;
        }

        public void Start()
        {
            lock(this.lockObject) {
                _receiver.EnvelopeReceived += OnEnvelopeReceived;
                _receiver.Start();
            }
        }

        public void Stop()
        {
            lock(this.lockObject) {
                _receiver.EnvelopeReceived -= OnEnvelopeReceived;
                _receiver.Stop();
            }
        }
    }
}
