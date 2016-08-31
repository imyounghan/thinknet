using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class DefaultProcessor : DisposableObject, IProcessor
    {
        private readonly IEnvelopeReceiver _receiver;

        private readonly Dictionary<string, IExecutor> executorDict;
        private readonly object lockObject;
        private bool started;

        public DefaultProcessor(IEnvelopeSender sender,
            IEnvelopeReceiver receiver,
            ICommandNotification notification, 
            IHandlerProvider handlerProvider,
            IHandlerRecordStore handlerStore,
            IEventBus eventBus,
            IEventPublishedVersionStore eventPublishedVersionStore,
            ISerializer serializer)
        {
            this._receiver = receiver;

            this.executorDict = new Dictionary<string, IExecutor>() {
                { "Command", new CommandExecutor(sender, handlerProvider) },
                { "Event", new EventExecutor(handlerStore, handlerProvider) },
                { "EventStream", new EventStreamExecutor(handlerProvider, eventBus, sender, eventPublishedVersionStore, serializer) },
                { "CommandReply", new CommandReplyExecutor(notification) }
            };
            this.lockObject = new object();
        }

        protected virtual string GetKind(object data)
        {
            if (data is EventStream)
                return "EventStream";

            if(data is CommandReply)
                return "CommandReply";

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

            envelope.Complete(sender);
        }

        public void Start()
        {
            ThrowIfDisposed();
            lock(this.lockObject) {
                if(!this.started) {
                    _receiver.EnvelopeReceived += OnEnvelopeReceived;
                    _receiver.Start();
                    this.started = true;
                }
            }
        }

        public void Stop()
        {
            lock(this.lockObject) {
                if(this.started) {
                    _receiver.EnvelopeReceived -= OnEnvelopeReceived;
                    _receiver.Stop();
                    this.started = false;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if(disposing) {
                this.Stop();
            }
        }
    }
}
