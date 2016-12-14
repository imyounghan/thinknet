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
    public class KafkaProcessor : DisposableObject, IProcessor, IInitializer
    {
        public const string OffsetPositionFile = "kafka.consumer.offset";

        private readonly ITextSerializer _serializer;
        private readonly IEnvelopeSender _sender;
        private readonly KafkaClient _kafka;
        private readonly Dictionary<string, ConcurrentDictionary<string, OffsetPosition>> offsetPositions;
        private readonly Dictionary<string, ConcurrentDictionary<int, long>> lastedOffsetPositions;
        private readonly Dictionary<string, SemaphoreSlim> topicSemaphores;
        private readonly Dictionary<string, Type> typeMaps;

        private readonly object lockObject;
        private CancellationTokenSource cancellationSource;

        public KafkaProcessor(ITextSerializer serializer, ITopicProvider topicProvider, IEnvelopeSender sender)
        {
            this.lockObject = new object();
            this._serializer = serializer;
            this._sender = sender;
            this._kafka = new KafkaClient(KafkaSettings.Current.ZookeeperAddress, topicProvider);
            this.offsetPositions = new Dictionary<string, ConcurrentDictionary<string, OffsetPosition>>();
            this.lastedOffsetPositions = new Dictionary<string, ConcurrentDictionary<int, long>>();
            this.topicSemaphores = new Dictionary<string, SemaphoreSlim>();
            this.typeMaps = new Dictionary<string, Type>();

            foreach(var topic in KafkaSettings.Current.SubscriptionTopics) {
                offsetPositions[topic] = new ConcurrentDictionary<string, OffsetPosition>();
                lastedOffsetPositions[topic] = new ConcurrentDictionary<int, long>();
                topicSemaphores[topic] = new SemaphoreSlim(KafkaSettings.Current.BufferCapacity, KafkaSettings.Current.BufferCapacity);
            }
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if(disposing) {
                this.Stop();
            }

            topicSemaphores.Clear();
            offsetPositions.Clear();
            lastedOffsetPositions.Clear();

            using(_kafka) { };
        }

        private void PullToLocal(object state)
        {
            string topic = state as string;

            while(!cancellationSource.IsCancellationRequested) {
                _kafka.Consume(cancellationSource.Token, topic, this.Deserialize, this.Distribute);
            }
        }

        private object Transform(GeneralData data)
        {
            Type type;
            if(!typeMaps.TryGetValue(data.TypeName, out type)) {
                type = data.GetMetadataType();
            }

            return _serializer.Deserialize(data.Metadata, type);
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
                if(command != null) {
                    envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
                    envelope.Metadata[StandardMetadata.SourceId] = command.Id;
                }

                var @event = envelope.Body as Event;
                if(@event != null) {
                    envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
                    envelope.Metadata[StandardMetadata.SourceId] = @event.Id;
                }
            }


            return envelope;
        }

        private void Distribute(Envelope envelope, string topic, OffsetPosition offset)
        {
            topicSemaphores[topic].Wait(cancellationSource.Token);
            offsetPositions[topic].TryAdd(envelope.GetMetadata(StandardMetadata.SourceId), offset);
            lastedOffsetPositions[topic][offset.PartitionId] = offset.Offset;
            envelope.Metadata["Topic"] = topic;
            
            _sender.Send(envelope);
        }

        private void OnEnvelopeCompleted(object sender, Envelope envelope)
        {
            var topic = envelope.GetMetadata("Topic");
            var id = envelope.GetMetadata(StandardMetadata.SourceId);
            OffsetPosition offset;

            if(!string.IsNullOrEmpty(id) && offsetPositions[topic].TryRemove(id, out offset)) {
                topicSemaphores[topic].Release();
                _kafka.CommitOffset(topic, offset);
            }
        }

        #region IProcessor 成员

        public void Start()
        {
            ThrowIfDisposed();

            lock(this.lockObject) {
                if(this.cancellationSource == null) {
                    Envelope.EnvelopeCompleted += OnEnvelopeCompleted;

                    this.cancellationSource = new CancellationTokenSource();
                    foreach(var topic in KafkaSettings.Current.SubscriptionTopics) {
                        Task.Factory.StartNew(this.PullToLocal,
                            topic,
                            this.cancellationSource.Token,
                            TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                            TaskScheduler.Current);
                    }
                }
            }
            
            Console.WriteLine("Kafka Processor Started.");
        }

        public void Stop()
        {
            ThrowIfDisposed();

            lock(this.lockObject) {
                if(this.cancellationSource != null) {
                    Envelope.EnvelopeCompleted -= OnEnvelopeCompleted;
                    using(this.cancellationSource) {
                        this.cancellationSource.Cancel();
                        this.cancellationSource = null;
                    }
                }
            }

            Console.WriteLine("Kafka Processor Stopped.");
        }

        #endregion


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

        private bool FilterType(Type type)
        {
            if(!type.IsClass)
                return false;

            if(type.IsAbstract)
                return false;

            return typeof(Command).IsAssignableFrom(type) || typeof(Event).IsAssignableFrom(type);
        }

        public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            assemblies.SelectMany(p => p.GetExportedTypes())
                .Where(FilterType).ForEach(delegate(Type type) {
                    typeMaps.Add(type.Name, type);
                });
        }

        #endregion
    }
}
