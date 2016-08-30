using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using KafkaNet.Protocol;
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
        private readonly Lazy<Producer> producer;
        private readonly Dictionary<string, Consumer> consumers;

        private readonly object lockObject;
        private CancellationTokenSource cancellationSource;
        private bool started;

        public KafkaService(ISerializer serializer, ITopicProvider topicProvider, IRoutingKeyProvider routingKeyProvider)
        {
            this._serializer = serializer;
            this._topicProvider = topicProvider;
            this._routingKeyProvider = routingKeyProvider;

            this.producer = new Lazy<Producer>(CreateKafkaProducer, LazyThreadSafetyMode.ExecutionAndPublication);
            this.consumers = new Dictionary<string, Consumer>();
            this.lockObject = new object();
        }

        private Producer CreateKafkaProducer()
        {
            var options = new KafkaOptions(KafkaSettings.Current.KafkaUris) {
                Log = KafkaLog.Instance
            };
            var router = new BrokerRouter(options);

            return new Producer(router);
        }

        private KafkaNet.Protocol.Message Serialize(object message)
        {
            string serialized;
            if (message is EventStream) {
                serialized = _serializer.Serialize(message);
            }
            else if (message is CommandReply) {
                serialized = _serializer.Serialize(message);
            }
            else {
                var metadata = new Dictionary<string, string>();
                var type = message.GetType();
                metadata[StandardMetadata.AssemblyName] = Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName);
                metadata[StandardMetadata.Namespace] = type.Namespace;
                metadata[StandardMetadata.TypeName] = type.Name;
                metadata["Playload"] = _serializer.Serialize(message);

                serialized = _serializer.Serialize(metadata);
            }


            return new KafkaNet.Protocol.Message(serialized);
        }

        public override Task SendAsync(Envelope envelope)
        {
            var topic = _topicProvider.GetTopic(envelope.Body);

            var message = this.Serialize(envelope.Body);
            return producer.Value.SendMessageAsync(topic, new[] { message });
        }

        private Task Push(KeyValuePair<string, KafkaNet.Protocol.Message[]> kvp)
        {
            return producer.Value.SendMessageAsync(kvp.Key, kvp.Value).ContinueWith(task => {
                if (task.IsFaulted) {
                    if (LogManager.Default.IsErrorEnabled) {
                        LogManager.Default.Error("send to kafka encountered error.", task.Exception);
                    }
                }
            });
        }

        private IDictionary<string, KafkaNet.Protocol.Message[]> GroupByTopic(IEnumerable<Envelope> envelopes)
        {
            return envelopes.AsParallel()
                .Select(item => new {
                    Topic = _topicProvider.GetTopic(item.Body),
                    Data = this.Serialize(item.Body)
                })
                .GroupBy(item => item.Topic)
                .ToDictionary(item => item.Key, item => item.Select(p => p.Data).ToArray());
        }

        public override Task SendAsync(IEnumerable<Envelope> envelopes)
        {
            return Task.Factory.StartNew(() => GroupByTopic(envelopes))
                .ContinueWith(task => {
                    var tasks = task.Result.Select(Push).ToArray();
                    Task.WaitAll(tasks);
                });
        }


        private void InitConsumers(KafkaOptions kafkaOptions)
        {
            var offsetPositions = new Dictionary<string, OffsetPosition[]>();

            try {
                var xml = new XmlDocument();
                xml.Load(OffsetPositionFile);

                offsetPositions = xml.DocumentElement.ChildNodes.Cast<XmlElement>().AsParallel()
                    .ToDictionary(topic => topic.GetAttribute("name"), topic => {
                        return topic.ChildNodes.Cast<XmlElement>()
                            .Select(offset => new OffsetPosition() {
                                PartitionId = offset.GetAttribute("partitionId").Change(0),
                                Offset = offset.InnerText.Change(0)
                            }).ToArray();
                    });
            }
            catch(Exception) {
                //TODO...Write LOG
            }

            foreach(var topic in KafkaSettings.Current.Topics) {
                consumers[topic] = new Consumer(new ConsumerOptions(topic, new BrokerRouter(kafkaOptions)) {
                    Log = KafkaLog.Instance
                }, offsetPositions.ContainsKey(topic) ? offsetPositions[topic] : new OffsetPosition[0]);
            }
        }

        private void RecordConsumerOffset()
        {
            var offsetPositions = OffsetPositionManager.Instance.Get();
            if (offsetPositions.Count == 0)
                return;

            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
            var root = xml.AppendChild(xml.CreateElement("root"));

            foreach (var kvp in offsetPositions) {
                var el = xml.CreateElement("topic");
                el.SetAttribute("name", kvp.Key);
                kvp.Value.ForEach(item => {
                    var node = xml.CreateElement("offset");
                    node.SetAttribute("partitionId", item.PartitionId.ToString());
                    node.InnerText = item.Offset.ToString();
                    el.AppendChild(node);
                });
                root.AppendChild(el);
            }
            xml.Save(OffsetPositionFile);
        }

        private void PullThenForward(object state)
        {
            string topic = state as string;
            var consumer = consumers[topic];
            var type = _topicProvider.GetType(topic);

            while(!cancellationSource.Token.IsCancellationRequested) {
                foreach(var message in consumer.Consume()) {
                    var serialized = message.Value.ToUtf8String();
                    try {
                        var envelope = this.Deserialize(serialized, type);
                        OffsetPositionManager.Instance.Add(topic, envelope.CorrelationId, message.Meta);
                        base.Distribute(envelope);
                    }
                    catch(Exception) {
                        //TODO...WriteLog
                    }
                }
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


            var routingKey = _routingKeyProvider.GetRoutingKey(message);
            return new Envelope() {
                Body = message,
                CorrelationId = message.Id,
                RoutingKey = routingKey,
            };
        }


        public void Start()
        {
            ThrowIfDisposed();
            lock(this.lockObject) {
                if(!this.started) {
                    if(this.cancellationSource == null) {
                        this.cancellationSource = new CancellationTokenSource();

                        foreach(var topic in KafkaSettings.Current.Topics) {
                            Task.Factory.StartNew(this.PullThenForward, 
                                topic, 
                                this.cancellationSource.Token,
                                TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                                TaskScheduler.Current);
                        }
                    }
                    this.started = true;
                }
            }
        }

        public void Stop()
        {
            lock(this.lockObject) {
                if(this.started) {
                    if(this.cancellationSource != null) {
                        using(this.cancellationSource) {
                            this.cancellationSource.Cancel();
                            this.cancellationSource = null;
                        }
                    }
                    this.started = false;
                }
            }
        }

        public void Initialize(IEnumerable<Type> types)
        {
            if(KafkaSettings.Current.Topics.IsEmpty())
                return;

            var offsetPositions = new Dictionary<string, OffsetPosition[]>();
            try {
                var xml = new XmlDocument();
                xml.Load(OffsetPositionFile);

                offsetPositions = xml.DocumentElement.ChildNodes.Cast<XmlElement>().AsParallel()
                    .ToDictionary(topic => topic.GetAttribute("name"), topic => {
                        return topic.ChildNodes.Cast<XmlElement>()
                            .Select(offset => new OffsetPosition() {
                                PartitionId = offset.GetAttribute("partitionId").Change(0),
                                Offset = offset.InnerText.Change(0)
                            }).ToArray();
                    });
            }
            catch(Exception) {
                //TODO...Write LOG
            }

            var kafkaOptions = new KafkaOptions(KafkaSettings.Current.KafkaUris);

            foreach(var topic in KafkaSettings.Current.Topics) {
                consumers[topic] = new Consumer(new ConsumerOptions(topic, new BrokerRouter(kafkaOptions)) {
                    Log = KafkaLog.Instance
                }, offsetPositions.ContainsKey(topic) ? offsetPositions[topic] : new OffsetPosition[0]);
            }
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if(disposing) {
                if(producer.IsValueCreated)
                    producer.Value.Dispose();

                consumers.Values.ForEach(consumer => consumer.Dispose());
            }
        }
    }
}
