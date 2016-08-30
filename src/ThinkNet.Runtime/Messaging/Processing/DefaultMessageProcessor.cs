using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class DefaultMessageProcessor : IProcessor
    {
        private readonly IEnvelopeReceiver _receiver;

        private readonly Dictionary<string, IExecutor> executorDict;
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

            this.executorDict = new Dictionary<string, IExecutor>() {
                { "Command", new CommandExecutor(notification, handlerProvider) },
                { "Event", new EventExecutor(handlerStore, handlerProvider) },
                { "EventStream", new SynchronousExecutor(notification, handlerProvider, eventBus, eventPublishedVersionStore, serializer) }
            };
            this.lockObject = new object();
        }

        protected void AddExecutor(string kind, IExecutor executor)
        {
            executorDict.Add(kind, executor);
        }

        protected virtual string GetKind(object data)
        {
            if (data is EventStream)
                return "EventStream";

            if (data is IEvent)
                return "Event";

            if (data is ICommand)
                return "Command";

            return string.Empty;
        }

        private void OnEnvelopeReceived(object sender, Envelope envelope)
        {
            var kind = this.GetKind(envelope.Body);
            if (string.IsNullOrEmpty(kind)) {
                //TODO...WriteLog
                return;
            }

            TimeSpan processTime;
            executorDict[kind].Execute(envelope.Body, out processTime);
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
