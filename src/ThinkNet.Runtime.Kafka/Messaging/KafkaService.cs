using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Configurations;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class KafkaService : EnvelopeHub, IProcessor, IInitializer
    {
        public const string OffsetPositionFile = "kafka.consumer.offset";

        private readonly ISerializer _serializer;
        private readonly ITopicProvider _topicProvider;

        private readonly object lockObject;
        private readonly KafkaClient kafka;
        private CancellationTokenSource cancellationSource;
        private bool started;
        private Timer timer;

        public KafkaService(ISerializer serializer, ITopicProvider topicProvider, IRoutingKeyProvider routingKeyProvider)
            : base(routingKeyProvider)
        {
            this._serializer = serializer;
            this._topicProvider = topicProvider;
            this.lockObject = new object();
            this.kafka = new KafkaClient(OffsetPositionFile, KafkaSettings.Current.KafkaUris);
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if (disposing)
                kafka.Dispose();
        }

        private GeneralData Transform(object data)
        {
            var serialized = _serializer.Serialize(data);
            return new GeneralData(data.GetType()) {
                Metadata = serialized
            };
        }

        private EventStream Transform(VersionedEvent @event)
        {
            @event.Events.ForEach(item => item.SourceId = string.Empty);
            var events = @event.Events.Select(Transform).ToArray();
            return new EventStream(@event.SourceId, @event.SourceType) {
                Id = @event.Id,
                CommandId = @event.CommandId,
                CreatedTime = @event.CreatedTime,
                Version = @event.Version,
                Events = events
            };
        }

        private string Serialize(Envelope envelope)
        {
            var kind = envelope.GetMetadata(StandardMetadata.Kind);
            switch (kind) {
                case StandardMetadata.VersionedEventKind:
                    var @event = envelope.Body as VersionedEvent;
                    return _serializer.Serialize(Transform(@event));
                case StandardMetadata.RepliedCommandKind:
                    return _serializer.Serialize(envelope.Body);
                default:
                    return _serializer.Serialize(Transform(envelope.Body));
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

            while(!cancellationSource.IsCancellationRequested) {
                kafka.Consume(topic, _topicProvider.GetType, this.Deserialize, this.GetIdentifierId, this.Distribute);
            }
        }

        private string GetIdentifierId(Envelope envelope)
        {
            return envelope.GetMetadata(StandardMetadata.IdentifierId)
                .IfEmpty(() => {
                    var message = envelope.Body as IMessage;
                    if(message != null)
                        return message.Id;

                    return string.Empty;
                });
        }


        private VersionedEvent Transform(EventStream @event)
        {
            var stream = new VersionedEvent(@event.Id, @event.CreatedTime) {
                SourceId = @event.SourceId,
                SourceType = @event.GetSourceType(),
                CommandId = @event.CommandId,
                Version = @event.Version,
                Events = @event.Events.Select(Transform).Cast<IEvent>().ToArray()
            };

            stream.Events.ForEach(item => item.SourceId = @event.SourceId);

            return stream;
        }

        private object Transform(GeneralData data)
        {
            return _serializer.Deserialize(data.Metadata, data.GetMetadataType());
        }
        private Envelope Deserialize(string serialized, Type type)
        {
            IMessage message;
            if(type == typeof(EventStream)) {
                var @event = _serializer.Deserialize<EventStream>(serialized);
                message = this.Transform(@event);
            }
            else if(type == typeof(RepliedCommand)) {
                message = _serializer.Deserialize<RepliedCommand>(serialized);
            }
            else {
                var data = _serializer.Deserialize<GeneralData>(serialized);
                message = (IMessage)this.Transform(data);
            }


            var envelope = new Envelope(message);
            envelope.Metadata[StandardMetadata.IdentifierId] = message.Id;
            if (type == typeof(EventStream)) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.VersionedEventKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((VersionedEvent)message).SourceId;
            }
            else if (type == typeof(RepliedCommand)) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.RepliedCommandKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((RepliedCommand)message).CommandId;
            }
            else if (TypeHelper.IsCommand(type)) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((ICommand)message).AggregateRootId;
            }
            else if (TypeHelper.IsEvent(type)) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((IEvent)message).SourceId;
            }

            return envelope;
        }

        private void OnEnvelopeCompleted(object sender, Envelope envelope)
        {
            var topic = _topicProvider.GetTopic(envelope.Body);
            kafka.RemoveOffset(topic, envelope.GetMetadata(StandardMetadata.IdentifierId));
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

            this.timer = new Timer(kafka.PersistConsumerOffset, null, 5000, 2000);
        }

        private void StoppingKafka()
        {
            this.timer.Dispose();
            this.timer = null;

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
