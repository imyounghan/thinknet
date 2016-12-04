using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThinkLib.Serialization;
using ThinkNet.Messaging;
using ThinkNet.Runtime.Kafka;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime
{
    public class KafkaService : EnvelopeHub, IProcessor
    {
       // public const string OffsetPositionFile = "kafka.consumer.offset";

        private readonly ITextSerializer _serializer;
        private readonly ITopicProvider _topicProvider;

        private readonly object lockObject;
        private readonly KafkaClient kafka;
        private readonly ConcurrentDictionary<string, TopicOffsetPosition> offsetPositions;
        private readonly BlockingCollection<Envelope> pendingQueue;
        private CancellationTokenSource cancellationSource;
        private bool started;

        public KafkaService(ITextSerializer serializer, ITopicProvider topicProvider, IRoutingKeyProvider routingKeyProvider)
            : base(routingKeyProvider)
        {
            this._serializer = serializer;
            this._topicProvider = topicProvider;
            this.lockObject = new object();
            this.kafka = new KafkaClient(KafkaSettings.Current.ZookeeperAddress);
            this.offsetPositions = new ConcurrentDictionary<string, TopicOffsetPosition>();
            this.pendingQueue = new BlockingCollection<Envelope>(ConfigurationSetting.Current.QueueCapacity);
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
                case StandardMetadata.MessageKind:
                    return _serializer.SerializeToBinary(envelope.Body, true);
                //case StandardMetadata.RepliedCommandKind:
                //    return _serializer.SerializeToBinary(envelope.Body);
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

        //private void PullThenForward(object state)
        //{
        //    string topic = state as string;

        //    while(!cancellationSource.IsCancellationRequested) {
        //        var type = _topicProvider.GetType(topic);
        //        kafka.Consume(cancellationSource.Token, topic, type, this.Deserialize, this.Distribute);
        //    }
        //}

        private void PullToLocal(object state)
        {
            string topic = state as string;

            while(!cancellationSource.IsCancellationRequested) {
                var type = _topicProvider.GetType(topic);
                kafka.Consume(cancellationSource.Token, topic, type, this.Deserialize, this.IntoMemory);
            }
        }

        private void IntoMemory(Envelope envelope, TopicOffsetPosition offset)
        {
            var id = envelope.GetMetadata(StandardMetadata.SourceId);

            if(offsetPositions.TryAdd(id, offset))
                pendingQueue.Add(envelope, cancellationSource.Token);
        }

        private void Distribute()
        {
            foreach(var item in pendingQueue.GetConsumingEnumerable(cancellationSource.Token)) {
                base.Route(item);
            }
        }
        
        private object Transform(GeneralData data)
        {
            return _serializer.Deserialize(data.Metadata, data.GetMetadataType());
        }
        private Envelope Deserialize(byte[] serialized, Type type)
        {
            var envelope = new Envelope();
            if(type == typeof(EventStream)) {
                var eventStream = _serializer.DeserializeFromBinary<EventStream>(serialized);
                envelope.Body = eventStream;
                //envelope.Metadata[StandardMetadata.IdentifierId] = ObjectId.GenerateNewStringId();
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.MessageKind;
                envelope.Metadata[StandardMetadata.SourceId] = eventStream.CorrelationId;
            }
            else if(type == typeof(CommandResult)) {
                var commandResult = _serializer.DeserializeFromBinary<CommandResult>(serialized);
                envelope.Body = commandResult;
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.MessageKind;
                envelope.Metadata[StandardMetadata.SourceId] = commandResult.CommandId;
            }
            else {
                var data = _serializer.DeserializeFromBinary<GeneralData>(serialized);
                envelope.Body = this.Transform(data);

                var command = envelope.Body as Command;
                if (command != null) {
                    envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
                    envelope.Metadata[StandardMetadata.SourceId] = command.Id;
                }

                var @event = envelope.Body as Event;
                if (@event != null) {
                    envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
                    envelope.Metadata[StandardMetadata.SourceId] = @event.Id;
                }
            }
           
            
            

            return envelope;
        }

        private void OnEnvelopeCompleted(object sender, Envelope envelope)
        {
            var id = envelope.GetMetadata(StandardMetadata.SourceId);
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
                    Task.Factory.StartNew(this.PullToLocal,
                        topic,
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                        TaskScheduler.Current);
                }

                Task.Factory.StartNew(this.Distribute,
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                        TaskScheduler.Current);
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
