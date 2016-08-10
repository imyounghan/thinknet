using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using KafkaNet.Protocol;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Runtime
{
    [Register(typeof(KafkaClient))]
    public class KafkaClient : DisposableObject, IInitializer
    {
        class KafkaLog : IKafkaLog
        {
            public static readonly IKafkaLog Instance = new KafkaLog();


            private readonly LogManager.ILogger logger;
            private KafkaLog()
            {
                this.logger = LogManager.GetLogger("Kafka");
            }


            #region IKafkaLog 成员

            public void DebugFormat(string format, params object[] args)
            {
                if (logger.IsDebugEnabled)
                    logger.DebugFormat(format, args);
            }

            public void ErrorFormat(string format, params object[] args)
            {
                if (logger.IsErrorEnabled)
                    logger.ErrorFormat(format, args);
            }

            public void FatalFormat(string format, params object[] args)
            {
                if (logger.IsFatalEnabled)
                    logger.FatalFormat(format, args);
            }

            public void InfoFormat(string format, params object[] args)
            {
                if (logger.IsInfoEnabled)
                    logger.InfoFormat(format, args);
            }

            public void WarnFormat(string format, params object[] args)
            {
                if (logger.IsWarnEnabled)
                    logger.WarnFormat(format, args);
            }

            #endregion
        }

        class KafkaTopicOffsetPosition
        {
            public string Topic { get; set; }

            public OffsetPosition[] Positions { get; set; }
        }

        private const string OffsetPositionFile = "kafka.consumer.offset";
        
        private readonly ISerializer serializer;
        private readonly IMetadataProvider metadataProvider;
        private readonly ITopicProvider topicProvider;

        private readonly Timer timer;
        private readonly Lazy<Producer> producer;
        private readonly Dictionary<string, Consumer> consumers;
        private readonly Dictionary<string, ConcurrentDictionary<string, MessageMetadata>> metadatas;
        private readonly Dictionary<string, ConcurrentDictionary<int, long>> lasted;

        public KafkaClient(ISerializer serializer, IMetadataProvider metadataProvider, ITopicProvider topicProvider)
        {
            this.serializer = serializer;
            this.metadataProvider = metadataProvider;
            this.topicProvider = topicProvider;

            var kafkaOptions = new KafkaOptions(KafkaSettings.Current.KafkaUris) {
                Log = KafkaLog.Instance
            };
            this.producer = new Lazy<Producer>(() => new Producer(new BrokerRouter(kafkaOptions)), LazyThreadSafetyMode.ExecutionAndPublication);

            this.consumers = new Dictionary<string, Consumer>();
            this.metadatas = new Dictionary<string, ConcurrentDictionary<string, MessageMetadata>>();
            this.lasted = new Dictionary<string, ConcurrentDictionary<int, long>>();

            foreach (var topic in KafkaSettings.Current.Topics) {
                consumers[topic] = new Consumer(new ConsumerOptions(topic, new BrokerRouter(kafkaOptions)) {
                    Log = KafkaLog.Instance
                }, GetCurrentOffsetPosition(topic));
                metadatas[topic] = new ConcurrentDictionary<string, MessageMetadata>();
                lasted[topic] = new ConcurrentDictionary<int, long>();
            }

            this.timer = new Timer(LongTask, this, 2000, 2000);
        }

        private OffsetPosition[] GetCurrentOffsetPosition(string topic)
        {
            string serialized = null;
            try {
                serialized = File.ReadAllText(string.Concat(OffsetPositionFile, ".", topic));
            }
            catch (Exception) {
            }

            if(string.IsNullOrEmpty(serialized))
                return new OffsetPosition[] { new OffsetPosition(0, 0) };

            return serializer.Deserialize<OffsetPosition[]>(serialized) ?? new OffsetPosition[] { new OffsetPosition(0, 0) };
        }


        private void LongTask(object state)
        {

            var xml = new System.Xml.XmlDocument();
            xml.CreateElement("topic");
            metadatas.AsParallel().ForAll(item => {
                OffsetPosition[] positions;
                if(item.Value.Count == 0) {
                    positions = lasted[item.Key].Select(p => new OffsetPosition(p.Key, p.Value + 1)).ToArray();
                }
                else {
                    positions = item.Value.Values
                        .GroupBy(p => p.PartitionId, p => p.Offset)
                        .Select(p => new OffsetPosition(p.Key, p.Min() + 1))
                        .ToArray();
                }
                var serialized = serializer.Serialize(positions, true);
                File.WriteAllText(string.Concat(OffsetPositionFile, ".", item.Key), serialized);
            });
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();
            if (disposing) {
                if (producer.IsValueCreated)
                    producer.Value.Dispose();

                foreach (var kvp in consumers) {
                    kvp.Value.Dispose();
                }
                consumers.Clear();

                timer.Dispose();
            }
        }

        public void Push(IEnumerable messages)
        {
            Dictionary<string, IList<Message>> dict = new Dictionary<string, IList<Message>>();

            for (IEnumerator item = messages.GetEnumerator(); item.MoveNext(); ) {
                var topic = topicProvider.GetTopic(item.Current);

                var message = this.Serialize(item.Current);
                dict.GetOrAdd(topic, () => new List<Message>()).Add(message);
            }

            foreach (var kvp in dict) {
                producer.Value.SendMessageAsync(kvp.Key, kvp.Value).ContinueWith(task => {
                    if (task.Exception != null) {
                        if (LogManager.Default.IsErrorEnabled) {
                            LogManager.Default.Error("send to kafka encountered error.", task.Exception);
                        }
                    }
                });
            }

            //var tasks = dict.Select(item => producer.Value.SendMessageAsync(item.Key, item.Value)).ToArray();
            //Task.WaitAll(tasks);
        }

        public void Pull(string topic, Action<object> action)
        {
            var consumer = consumers[topic];
            var metadata = metadatas[topic];
            var position = lasted[topic];

            foreach (var message in consumer.Consume()) {
                object item;
                position.AddOrUpdate(message.Meta.PartitionId, message.Meta.Offset, (key, value) => message.Meta.Offset);
                if (this.Deserialize(message, metadata, out item)) {
                    action(item);
                }
            }
        }



        private bool Deserialize(Message message, ConcurrentDictionary<string, MessageMetadata> dict, out object obj)
        {
            var serialized = message.Value.ToUtf8String();
            if (!serialized.StartsWith("{") && !serialized.EndsWith("}")) {
                obj = DBNull.Value;
                return false;
            }

            var metadata = serializer.Deserialize<IDictionary<string, string>>(serialized);

            string uniqueId;
            if (metadata.TryGetValue(StandardMetadata.UniqueId, out uniqueId)) {
                dict.TryAdd(uniqueId, message.Meta);
            }

            var typeFullName = string.Format("{0}.{1}, {2}",
                metadata[StandardMetadata.Namespace],
                metadata[StandardMetadata.TypeName],
                metadata[StandardMetadata.AssemblyName]);
            var type = Type.GetType(typeFullName);

            obj = serializer.Deserialize(metadata["Playload"], type);

            return true;
        }

        private Message Serialize(object message)
        {
            var metadata = metadataProvider.GetMetadata(message);
            metadata["Playload"] = serializer.Serialize(message);

            return new Message(serializer.Serialize(metadata));
        }

        public void ConsumerComplete(string topic, string messageId)
        {
            metadatas[topic].Remove(messageId);
        }

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            using (var router = new BrokerRouter(new KafkaOptions(KafkaSettings.Current.KafkaUris))) {
                int count = -1;
                while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
                    try {
                        var result = router.GetTopicMetadata(KafkaSettings.Current.Topics);
                        if (result.All(topic => topic.ErrorCode == (short)ErrorResponseCode.NoError))
                            break;

                        result.Where(topic => topic.ErrorCode != (short)ErrorResponseCode.NoError)
                            .ForEach(topic => {
                                if (LogManager.Default.IsWarnEnabled)
                                    LogManager.Default.WarnFormat("get the topic('{0}') of status is {1}.",
                                        topic.Name, (ErrorResponseCode)topic.ErrorCode);
                            });


                        Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
                    }
                    catch (Exception) {
                        throw;
                    }
                }
            }
        }
        #endregion
    }
}
