﻿using System;
using System.Collections.Generic;
using ThinkNet.EventSourcing;
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
            IEventPublishedVersionStore eventPublishedVersionStore)
        {
            this._receiver = receiver;

            this.executorDict = new Dictionary<string, IExecutor>() {
                { StandardMetadata.CommandKind, new CommandExecutor(sender, handlerProvider, handlerStore) },
                { StandardMetadata.EventKind, new EventExecutor(handlerStore, handlerProvider) },
                { StandardMetadata.VersionedEventKind, new VersionedEventExecutor(handlerProvider, eventBus, sender, eventPublishedVersionStore) },
                { StandardMetadata.RepliedCommandKind, new RepliedCommandExecutor(notification) }
            };
            this.lockObject = new object();
        }

        protected virtual string GetKind(object data)
        {
            if (data is VersionedEvent)
                return StandardMetadata.VersionedEventKind;

            if(data is RepliedCommand)
                return StandardMetadata.RepliedCommandKind;

            if (data is IEvent)
                return StandardMetadata.EventKind;

            if (data is ICommand)
                return StandardMetadata.CommandKind;

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
