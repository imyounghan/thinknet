using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Configurations;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class KafkaService : EnvelopeHub, IProcessor
    {
       // public const string OffsetPositionFile = "kafka.consumer.offset";

        private readonly ISerializer _serializer;
        private readonly ITopicProvider _topicProvider;

        private readonly object lockObject;
        private readonly KafkaClient kafka;
        private readonly ConcurrentDictionary<string, TopicOffsetPosition> offsetPositions;
        private CancellationTokenSource cancellationSource;
        private bool started;

        public KafkaService(ISerializer serializer, ITopicProvider topicProvider, IRoutingKeyProvider routingKeyProvider)
            : base(routingKeyProvider)
        {
            this._serializer = serializer;
            this._topicProvider = topicProvider;
            this.lockObject = new object();
            this.kafka = new KafkaClient(KafkaSettings.Current.ZookeeperAddress);
            this.offsetPositions = new ConcurrentDictionary<string, TopicOffsetPosition>();
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

        private byte[] Serialize(Envelope envelope)
        {
            var kind = envelope.GetMetadata(StandardMetadata.Kind);
            switch (kind) {
                case StandardMetadata.VersionedEventKind:
                    return _serializer.SerializeToBinary(envelope.Body, true);
                case StandardMetadata.RepliedCommandKind:
                    return _serializer.SerializeToBinary(envelope.Body);
                default:
                    return _serializer.SerializeToBinary(Transform(envelope.Body));
            }
        }

        private string GetTopic(Envelope envelope)
        {
            return _topicProvider.GetTopic(envelope.Body);
        }

        public override Task SendAsync(Envelope envelope)
        {
            //return this.SendAsync(new[] { envelope });
            return kafka.Push(GetTopic(envelope), new[] { envelope }, this.Serialize);
        }

        public override Task SendAsync(IEnumerable<Envelope> envelopes)
        {
            return kafka.Push(envelopes, GetTopic, this.Serialize);
        }

        private void PullThenForward(object state)
        {
            string topic = state as string;

            while(!cancellationSource.IsCancellationRequested) {
                var type = _topicProvider.GetType(topic);
                kafka.Consume(cancellationSource.Token, topic, type, this.Deserialize, this.Distribute);
            }
        }

        private void Distribute(Envelope envelope, TopicOffsetPosition offset)
        {
            var id = envelope.GetMetadata(StandardMetadata.IdentifierId);

            if(offsetPositions.TryAdd(id, offset))
                base.Distribute(envelope);
        }
        
        //private VersionedEvent Transform(EventStream @event)
        //{
        //    var stream = new VersionedEvent(@event.Id, @event.CreatedTime) {
        //        SourceId = @event.SourceId,
        //        SourceType = @event.GetSourceType(),
        //        CommandId = @event.CommandId,
        //        Version = @event.Version,
        //        Events = @event.Events.Select(Transform).Cast<IEvent>().ToArray()
        //    };

        //    stream.Events.ForEach(item => item.SourceId = @event.SourceId);

        //    return stream;
        //}

        private object Transform(GeneralData data)
        {
            return _serializer.Deserialize(data.Metadata, data.GetMetadataType());
        }
        private Envelope Deserialize(byte[] serialized, Type type)
        {
            var envelope = new Envelope();
            IMessage message;
            if(type == typeof(VersionedEvent)) {
                envelope.Body = message = _serializer.DeserializeFromBinary<VersionedEvent>(serialized);
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.VersionedEventKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((VersionedEvent)message).SourceId;
            }
            else if(type == typeof(RepliedCommand)) {
                envelope.Body = message = _serializer.DeserializeFromBinary<RepliedCommand>(serialized);
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.RepliedCommandKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((RepliedCommand)message).CommandId;
            }
            else {
                var data = _serializer.DeserializeFromBinary<GeneralData>(serialized);
                envelope.Body = message = this.Transform(data) as IMessage;

                if(TypeHelper.IsCommand(type)) {
                    envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
                    envelope.Metadata[StandardMetadata.SourceId] = ((ICommand)message).AggregateRootId;
                }
                else if(TypeHelper.IsEvent(type)) {
                    envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
                    envelope.Metadata[StandardMetadata.SourceId] = ((IEvent)message).SourceId;
                }
            }
           
            if(message == null) {
                envelope.Metadata[StandardMetadata.IdentifierId] = ObjectId.GenerateNewStringId();
            }
            else {
                envelope.Metadata[StandardMetadata.IdentifierId] = message.Id;
            }
            
            

            return envelope;
        }

        private void OnEnvelopeCompleted(object sender, Envelope envelope)
        {
            var id = envelope.GetMetadata(StandardMetadata.IdentifierId);
            TopicOffsetPosition offset;

            if(!string.IsNullOrEmpty(id) && offsetPositions.TryRemove(id, out offset))
                kafka.CommitOffset(offset);
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
            //this.timer.Dispose();
            //this.timer = null;

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
    }
}
