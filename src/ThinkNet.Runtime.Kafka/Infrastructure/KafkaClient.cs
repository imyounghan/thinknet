using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using KafkaNet.Protocol;

namespace ThinkNet.Infrastructure
{
    public class KafkaClient : DisposableObject
    {
        private readonly string _kafkaOffsetFile;
        private readonly KafkaOptions _kafkaOption;
        private readonly Lazy<Producer> _kafkaProducer;
        private readonly ConcurrentDictionary<string, Consumer> _kafkaConsumers;

        private readonly Dictionary<string, ConcurrentDictionary<string, MessageMetadata>> metadatas;
        private readonly Dictionary<string, ConcurrentDictionary<int, long>> lasted;
        private readonly Dictionary<string, OffsetPosition[]> final;


        public KafkaClient(string offsetFile, params Uri[] kafkaUris)
        {
            offsetFile.NotNullOrEmpty("offsetFile");
            if (kafkaUris == null || kafkaUris.Length == 0) {
                throw new ArgumentNullException("kafkaUris");
            }

            this._kafkaOffsetFile = offsetFile; 
            this._kafkaOption = new KafkaOptions(kafkaUris) {
                Log = KafkaLog.Instance
            };

            this._kafkaProducer = new Lazy<Producer>(() => new Producer(new BrokerRouter(_kafkaOption)), true);
            this._kafkaConsumers = new ConcurrentDictionary<string, Consumer>();

            this.metadatas = new Dictionary<string, ConcurrentDictionary<string, MessageMetadata>>();
            this.lasted = new Dictionary<string, ConcurrentDictionary<int, long>>();
            this.final = new Dictionary<string, OffsetPosition[]>();
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();
            if(disposing) {
                if (_kafkaProducer.IsValueCreated)
                    _kafkaProducer.Value.Dispose();

                foreach (var consumer in _kafkaConsumers) {
                    consumer.Value.Dispose();
                }
                _kafkaConsumers.Clear();
            }
        }

        //public void EnsureProducerTopic(params string[] topics)
        //{
        //    foreach(var topic in topics) {
        //        int count = -1;
        //        while(count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
        //            try {
        //                Topic result = producer.Value.GetTopic(topic);
        //                if(result.ErrorCode == (short)ErrorResponseCode.NoError) {
        //                    break;
        //                }

        //                Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
        //            }
        //            catch(Exception) {
        //            }
        //        }
        //    }
        //}

        //public void EnsureConsumerTopic(params string[] topics)
        //{
        //    foreach(var topic in topics) {
        //        int count = -1;
        //        var consumer = new Consumer(new ConsumerOptions(topic, router));
        //        while(count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
        //            try {
        //                Topic result = consumer.GetTopic(topic);
        //                if(result.ErrorCode == (short)ErrorResponseCode.NoError) {
        //                    consumers.TryAdd(topic, new KafkaConsumer(consumer, topic));
        //                    break;
        //                }

        //                if(LogManager.Default.IsDebugEnabled)
        //                    LogManager.Default.DebugFormat("get the topic('{0}') of status is {1}", topic, (ErrorResponseCode)result.ErrorCode);

        //                Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
        //            }
        //            catch(Exception) {
        //            }
        //        }
        //    }
        //}

        private Task PushToKafka(string topic , IEnumerable<Message> messages)
        {
            if (LogManager.Default.IsDebugEnabled) {
                LogManager.Default.Debug("ready to send a message to kafka.");
            }

            return _kafkaProducer.Value.SendMessageAsync(topic, messages).ContinueWith(task => {
                if (task.IsFaulted) {
                    if (LogManager.Default.IsErrorEnabled) {
                        LogManager.Default.Error("send to kafka encountered error.", task.Exception);
                    }
                }
            });
        }

        public Task Push<T>(T element, Func<T, string> getTopic, Func<T, string> serializer)
        {
            return this.Push(getTopic(element), element, serializer);
        }
        public Task Push<T>(string topic, T element, Func<T, string> serializer)
        {
            return this.PushToKafka(topic, new[] { new Message(serializer(element)) });
        }
        public Task Push<T>(string topic, IEnumerable<T> elements, Func<T, string> serializer)
        {
            return this.PushToKafka(topic, elements.Select(item => new Message(serializer(item))).ToArray());
        }
        public Task Push<T>(IEnumerable<T> elements, Func<T, string> getTopic, Func<T, string> serializer)
        {
            return Task.Factory.StartNew(() => {
                var tasks = elements.AsParallel()
                    .GroupBy(item => getTopic(item))
                    .Select(group => {
                        var messages = group.Select(item => new Message(serializer(item))).ToArray();
                        return this.PushToKafka(group.Key, messages);
                    }).ToArray();
                Task.WaitAll(tasks);
            });
        }

        private void Add(string topic, string correlationId, MessageMetadata meta)
        {
            lasted[topic].AddOrUpdate(meta.PartitionId, meta.Offset, (key, value) => meta.Offset);
            if(!string.IsNullOrEmpty(correlationId)) {
                metadatas[topic].TryAdd(correlationId, meta);
            }
        }

        public void UpdateOffset(string topic, string correlationId)
        {
            if(!string.IsNullOrEmpty(correlationId)) {
                metadatas[topic].Remove(correlationId);
            }
        }

        private IDictionary<string, OffsetPosition[]> GetOffsetPositions()
        {
            if(metadatas.All(p => p.Value.Count == 0) && lasted.All(p => p.Value.Count == 0)) {
                final.Clear();
                return final;
            }

            foreach (var topic in _kafkaConsumers.Keys) {
                OffsetPosition[] positions;
                if (metadatas[topic].Count == 0) {
                    positions = lasted[topic].Select(p => new OffsetPosition(p.Key, p.Value + 1)).ToArray();
                }
                else {
                    positions = metadatas[topic].Values
                        .GroupBy(p => p.PartitionId, p => p.Offset)
                        .Select(p => new OffsetPosition(p.Key, p.Min() + 1))
                        .ToArray();
                }

                final[topic] = positions;
            }

            return final;
        }

        public void Consume(string topic, Action<string> action)
        {
            foreach (var message in _kafkaConsumers[topic].Consume()) {
                var serialized = message.Value.ToUtf8String();
                action(serialized);
            }
        }

        private void RecordConsumerOffset(object state)
        {
            var offsetPositions = this.GetOffsetPositions();
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
            xml.Save(_kafkaOffsetFile);
        }

        public void InitConsumers(params string[] topics)
        {
            if (topics == null || topics.Length == 0)
                return;

            var offsetPositions = new Dictionary<string, OffsetPosition[]>();

            try {
                var xml = new XmlDocument();
                xml.Load(_kafkaOffsetFile);

                offsetPositions = xml.DocumentElement.ChildNodes.Cast<XmlElement>().AsParallel()
                    .ToDictionary(topic => topic.GetAttribute("name"), topic => {
                        return topic.ChildNodes.Cast<XmlElement>()
                            .Select(offset => new OffsetPosition() {
                                PartitionId = offset.GetAttribute("partitionId").Change(0),
                                Offset = offset.InnerText.Change(0)
                            }).ToArray();
                    });
            }
            catch (Exception) {
                //TODO...Write LOG
            }

            foreach (var topic in topics) {
                _kafkaConsumers[topic] = new Consumer(new ConsumerOptions(topic, new BrokerRouter(_kafkaOption)) {
                    Log = KafkaLog.Instance
                }, offsetPositions.ContainsKey(topic) ? offsetPositions[topic] : new OffsetPosition[0]);
            }
        }
    }
}
