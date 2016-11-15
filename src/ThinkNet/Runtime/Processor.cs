using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common;
using ThinkNet.Contracts;
using ThinkNet.Domain;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime.Executing;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime
{
    //[Register("core", typeof(IProcessor))]
    public class Processor : DisposableObject, IProcessor, IInitializer
    {
        private readonly IEnvelopeReceiver _receiver;

        private readonly Dictionary<string, IExecutor> executorDict;
        private readonly object lockObject;
        private bool started;

        public Processor(IRepository repository,
            IEventSourcedRepository eventSourcedRepository,
            IEnvelopeSender sender,
            IEnvelopeReceiver receiver,
            ICommandResultNotification notification,
            IMessageHandlerRecordStore handlerStore,
            IMessageBus messageBus)
        {
            this._receiver = receiver;

            this.executorDict = new Dictionary<string, IExecutor>(StringComparer.CurrentCulture) {
                { StandardMetadata.CommandKind, new CommandExecutor(repository, eventSourcedRepository,messageBus, handlerStore) },
                { StandardMetadata.EventKind, new EventExecutor(handlerStore) },
                { StandardMetadata.MessageKind, new MessageExecutor(sender, handlerStore, messageBus, notification) }
            };
            this.lockObject = new object();
        }

        protected void AddExecutor(string kind, IExecutor executor)
        {
            if (executorDict.ContainsKey(kind))
                return;

            executorDict[kind] = executor;
        }

        protected virtual string GetKind(object data)
        {
            if (data is IEvent)
                return StandardMetadata.EventKind;

            if (data is Messaging.ICommand)
                return StandardMetadata.CommandKind;

            if (data is IMessage)
                return StandardMetadata.MessageKind;

            return string.Empty;
        }


        private void OnEnvelopeReceived(object sender, Envelope envelope)
        {
            var kind = envelope.GetMetadata(StandardMetadata.Kind);
            if (string.IsNullOrEmpty(kind)) {
                kind = this.GetKind(envelope.Body);
            }
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
            lock (this.lockObject) {
                if (!this.started) {
                    _receiver.EnvelopeReceived += OnEnvelopeReceived;
                    _receiver.Start();
                    this.started = true;
                }
            }
        }

        public void Stop()
        {
            lock (this.lockObject) {
                if (this.started) {
                    _receiver.EnvelopeReceived -= OnEnvelopeReceived;
                    _receiver.Stop();
                    this.started = false;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if (disposing) {
                this.Stop();
            }
        }

        public void Initialize(IEnumerable<Type> types)
        {
            executorDict.Values.OfType<IInitializer>().ForEach(delegate(IInitializer initializer) {
                initializer.Initialize(types);
            });
        }
    }
}
