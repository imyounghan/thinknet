using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime.Kafka
{
    public class KafkaService : EnvelopeHub, IInitializer
    {
        public const string OffsetPositionFile = "kafka.consumer.offset";

        private readonly ITextSerializer _serializer;
        private readonly KafkaClient kafka;
        private readonly Dictionary<string, ConcurrentDictionary<string, OffsetPosition>> offsetPositions;
        private readonly Dictionary<string, ConcurrentDictionary<int, long>> lastedOffsetPositions;
        private readonly Dictionary<string, SemaphoreSlim> topicSemaphores;

        private CancellationTokenSource cancellationSource;

        public KafkaService(ITextSerializer serializer, ITopicProvider topicProvider, IRoutingKeyProvider routingKeyProvider)
            : base(routingKeyProvider)
        {
            this._serializer = serializer;
            this.kafka = new KafkaClient(KafkaSettings.Current.ZookeeperAddress, topicProvider);
            this.offsetPositions = new Dictionary<string, ConcurrentDictionary<string, OffsetPosition>>();
            this.lastedOffsetPositions = new Dictionary<string, ConcurrentDictionary<int, long>>();
            this.topicSemaphores = new Dictionary<string, SemaphoreSlim>();

            foreach(var topic in KafkaSettings.Current.SubscriptionTopics) {
                offsetPositions[topic] = new ConcurrentDictionary<string, OffsetPosition>();
                lastedOffsetPositions[topic] = new ConcurrentDictionary<int, long>();
                topicSemaphores[topic] = new SemaphoreSlim(KafkaSettings.Current.BufferCapacity, KafkaSettings.Current.BufferCapacity);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(!disposing)
                return;

            this.StoppingKafka(); 
            kafka.Dispose();

            Console.WriteLine("Kafka service is stopped.");
        }

        private GeneralData Transform(object data)
        {
            var serialized = _serializer.Serialize(data);
            return new GeneralData(data.GetType()) {
                Metadata = serialized
            };
        }

        private byte[] Serialize(object element)
        {
            var eventCollection = element as EventCollection;
            if(eventCollection != null) {
                var events = eventCollection.Select(Transform).ToArray();
                var stream = new EventStream() {
                    CorrelationId = eventCollection.CorrelationId,
                    Events = eventCollection.Select(Transform).ToArray(),
                    SourceAssemblyName = eventCollection.SourceId.AssemblyName,
                    SourceId = eventCollection.SourceId.Id,
                    SourceNamespace = eventCollection.SourceId.Namespace,
                    SourceTypeName = eventCollection.SourceId.TypeName,
                    Version = eventCollection.Version
                };

                return _serializer.SerializeToBinary(stream);
            }

            var commandResult = element as CommandResult;
            if(commandResult != null) {
                return _serializer.SerializeToBinary(commandResult);
            }


            return _serializer.SerializeToBinary(Transform(element));
        }


        public override Task SendAsync(Envelope envelope)
        {
            if(envelope.GetMetadata(StandardMetadata.Kind) == StandardMetadata.QueryKind) {
                if(LogManager.Default.IsDebugEnabled) {
                    LogManager.Default.DebugFormat("Send an envelope to local queue, data({0}).", envelope.Body);
                }

                return Task.Factory.StartNew(() => this.Route(envelope));
            }

            return this.SendAsync(new[] { envelope });
        }

        public override Task SendAsync(IEnumerable<Envelope> envelopes)
        {
            return kafka.Push(envelopes.Select(item => item.Body), this.Serialize);
        }
        
        private void PullToLocal(object state)
        {
            string topic = state as string;

            while(!cancellationSource.IsCancellationRequested) {
                kafka.Consume(cancellationSource.Token, topic, this.Deserialize, this.Distribute);
            }
        }
        
        private void Distribute(Envelope envelope, string topic, OffsetPosition offset)
        {
            topicSemaphores[topic].Wait(cancellationSource.Token);
            offsetPositions[topic].TryAdd(envelope.GetMetadata(StandardMetadata.SourceId), offset);
            lastedOffsetPositions[topic][offset.PartitionId] = offset.Offset;
            envelope.Metadata["Topic"] = topic;

            if(LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Distribute an envelope to local queue. topic:{0}, offset:{1}, partition:{2}, data:({3}).", topic, offset.Offset, offset.PartitionId, envelope.Body);
            }
            this.Route(envelope);
        }
        
        private object Transform(GeneralData data)
        {
            return _serializer.Deserialize(data.Metadata, data.GetMetadataType());
        }
        private Envelope Deserialize(byte[] serialized, Type type)
        {
            var envelope = new Envelope();
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.MessageKind;
            if(type == typeof(EventStream)) {
                var stream = _serializer.DeserializeFromBinary<EventStream>(serialized);
                var events = stream.Events.Select(this.Transform).Cast<Event>();
                envelope.Body = new EventCollection(events) {
                    CorrelationId = stream.CorrelationId,
                    SourceId = new SourceKey(stream.SourceId, stream.SourceNamespace, stream.SourceTypeName, stream.SourceAssemblyName),
                    Version = stream.Version
                };
                envelope.Metadata[StandardMetadata.SourceId] = stream.CorrelationId;
            }
            else if(type == typeof(CommandResult)) {
                var commandResult = _serializer.DeserializeFromBinary<CommandResult>(serialized);
                envelope.Body = commandResult;                
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
            var topic = envelope.GetMetadata("Topic");
            var id = envelope.GetMetadata(StandardMetadata.SourceId);
            OffsetPosition offset;

            if(envelope.GetMetadata(StandardMetadata.Kind) != StandardMetadata.QueryKind && 
                !string.IsNullOrEmpty(id) && offsetPositions[topic].TryRemove(id, out offset)) {                
                topicSemaphores[topic].Release();
                kafka.CommitOffset(topic, offset);
            }
        }

        private void StartingKafka()
        {
            Envelope.EnvelopeCompleted += OnEnvelopeCompleted;

            if(this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();

                foreach (var topic in KafkaSettings.Current.SubscriptionTopics) {
                    Task.Factory.StartNew(this.PullToLocal,
                        topic,
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                        TaskScheduler.Current);
                }
            }

            Console.WriteLine("Kafka service is started.");
        }

        private void StoppingKafka()
        {
            Envelope.EnvelopeCompleted -= OnEnvelopeCompleted;

            if(this.cancellationSource != null) {
                using(this.cancellationSource) {
                    this.cancellationSource.Cancel();
                    this.cancellationSource = null;
                }
            }
        }

        private void RecordConsumerOffset()
        {
            if(offsetPositions.All(p => p.Value.Count == 0) && lastedOffsetPositions.All(p => p.Value.Count == 0)) {
                return;
            }

            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
            var root = xml.AppendChild(xml.CreateElement("root"));


            foreach(var topic in KafkaSettings.Current.SubscriptionTopics) {
                var history = offsetPositions[topic].Values;
                OffsetPosition[] array = new OffsetPosition[0];
                if(history.Count == 0) {
                    var last = lastedOffsetPositions[topic];
                    if(last.Count > 0) {
                        array = last.Select(p => new OffsetPosition(p.Key, p.Value + 1)).ToArray();
                    }
                }
                else {
                    array = GetLastOffsetByPartition(history);
                }

                if(array.Length == 0)
                    continue;

                var el = xml.CreateElement("topic");
                el.SetAttribute("name", topic);
                foreach(var item in array) {
                    var node = xml.CreateElement("offset");
                    node.SetAttribute("partitionId", item.PartitionId.ToString());
                    node.InnerText = item.Offset.ToString();
                    el.AppendChild(node);
                }
                root.AppendChild(el);
            }

            xml.Save(OffsetPositionFile);
        }

        private OffsetPosition[] GetLastOffsetByPartition(ICollection<OffsetPosition> offsetPositionCollection)
        {
            return offsetPositionCollection.GroupBy(p => p.PartitionId, p => p.Offset)
                .Select(p => new OffsetPosition(p.Key, p.Min() + 1)).ToArray();
        }

        #region IInitializer 成员

        public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            this.StartingKafka();
        }

        #endregion
    }
}
