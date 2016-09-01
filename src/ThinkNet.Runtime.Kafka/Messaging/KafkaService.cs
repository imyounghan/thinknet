using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class KafkaService : EnvelopeHub, IProcessor, IInitializer
    {
        public const string OffsetPositionFile = "kafka.consumer.offset";

        private readonly ISerializer _serializer;
        private readonly ITopicProvider _topicProvider;
        private readonly IRoutingKeyProvider _routingKeyProvider;

        private readonly object lockObject;
        private readonly KafkaClient kafka;
        private CancellationTokenSource cancellationSource;
        private bool started;

        public KafkaService(ISerializer serializer, ITopicProvider topicProvider, IRoutingKeyProvider routingKeyProvider)
        {
            this._serializer = serializer;
            this._topicProvider = topicProvider;
            this._routingKeyProvider = routingKeyProvider;
            this.lockObject = new object();
            this.kafka = new KafkaClient(OffsetPositionFile, KafkaSettings.Current.KafkaUris);
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if (disposing)
                kafka.Dispose();
        }
       
        private string Serialize(Envelope envelope)
        {
            var kind = envelope.GetMetadata(StandardMetadata.Kind);
            switch (kind) {
                case StandardMetadata.EventStreamKind:
                case StandardMetadata.CommandReplyKind:
                    return _serializer.Serialize(envelope.Body);
                default:
                    var metadata = new Dictionary<string, string>();
                    metadata[StandardMetadata.AssemblyName] = envelope.GetMetadata(StandardMetadata.AssemblyName);
                    metadata[StandardMetadata.Namespace] = envelope.GetMetadata(StandardMetadata.Namespace);
                    metadata[StandardMetadata.TypeName] = envelope.GetMetadata(StandardMetadata.TypeName);
                    metadata["Playload"] = _serializer.Serialize(envelope.Body);
                    return _serializer.Serialize(metadata);
            }
        }

        private string GetTopic(Envelope envelope)
        {
            return _topicProvider.GetTopic(envelope.Body);
        }

        public override Task SendAsync(Envelope envelope)
        {
            return kafka.Push(envelope, GetTopic, Serialize);
        }

        public override Task SendAsync(IEnumerable<Envelope> envelopes)
        {
            return kafka.Push(envelopes, GetTopic, Serialize);
        }

        private void PullThenForward(object state)
        {
            string topic = state as string;
            var type = _topicProvider.GetType(topic);

            while(!cancellationSource.IsCancellationRequested) {
                kafka.Consume(topic, serialized => {
                    try {
                        var envelope = this.Deserialize(serialized, type);
                        base.Distribute(envelope);
                    }
                    catch (Exception) {
                        //TODO...WriteLog
                    }
                });
            }
        }

        private Envelope Deserialize(string serialized, Type type)
        {
            IMessage message;
            if(type == typeof(EventStream) || type == typeof(CommandReply)) {
                message = (IMessage)_serializer.Deserialize(serialized, type);
            }
            else {
                var metadata = (IDictionary<string, string>)_serializer.Deserialize(serialized, type);
                var typeFullName = string.Format("{0}.{1}, {2}",
                    metadata[StandardMetadata.Namespace],
                    metadata[StandardMetadata.TypeName],
                    metadata[StandardMetadata.AssemblyName]);
                message = (IMessage)_serializer.Deserialize(metadata["Playload"], Type.GetType(typeFullName, true));
            }


            var envelope = new Envelope(message);
            envelope.Metadata[StandardMetadata.CorrelationId] = message.Id;
            envelope.Metadata[StandardMetadata.RoutingKey] = _routingKeyProvider.GetRoutingKey(message);

            if (type == typeof(EventStream)) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventStreamKind;
            }
            else if (type == typeof(CommandReply)) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventStreamKind;
            }
            else if (TypeHelper.IsCommand(type)) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
            }
            else if (TypeHelper.IsEvent(type)) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
            }

            return envelope;
        }

        private void OnEnvelopeCompleted(object sender, Envelope envelope)
        {
            var topic = _topicProvider.GetTopic(envelope.Body);
            kafka.UpdateOffset(topic, envelope.GetMetadata(StandardMetadata.CorrelationId));
        }

        private void StartingKafka()
        {
            Envelope.EnvelopeCompleted += OnEnvelopeCompleted;

            if (this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();

                foreach (var topic in KafkaSettings.Current.SubscriptionTopics) {
                    Task.Factory.StartNew(this.PullThenForward,
                        topic,
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                        TaskScheduler.Current);
                }
            }
        }

        private void StoppingKafka()
        {
            Envelope.EnvelopeCompleted -= OnEnvelopeCompleted;

            if (this.cancellationSource != null) {
                using (this.cancellationSource) {
                    this.cancellationSource.Cancel();
                    this.cancellationSource = null;
                }
            }
        }

        #region IProcessor 成员

        void IProcessor.Start()
        {
            ThrowIfDisposed();
            lock (this.lockObject) {
                if (!this.started) {
                    this.StartingKafka();
                    this.started = true;
                }
            }
        }

        void IProcessor.Stop()
        {
            lock (this.lockObject) {
                if (this.started) {
                    this.StoppingKafka();
                    this.started = false;
                }
            }
        }

        #endregion

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            kafka.InitConsumers(KafkaSettings.Current.SubscriptionTopics);
        }

        #endregion
    }
}
