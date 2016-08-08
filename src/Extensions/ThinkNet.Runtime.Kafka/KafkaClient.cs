using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using KafkaNet.Protocol;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Runtime
{
    [Register(typeof(KafkaClient))]
    public class KafkaClient : DisposableObject
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
        
        
        private readonly ISerializer serializer;
        private readonly IMetadataProvider metadataProvider;
        private readonly ITopicProvider topicProvider;

        private readonly Lazy<Producer> producer;
        private readonly Dictionary<string, Consumer> consumers;
        private readonly Dictionary<string, ConcurrentDictionary<string, MessageMetadata>> metadatas;

        public KafkaClient(ISerializer serializer, IMetadataProvider metadataProvider, ITopicProvider topicProvider)
        {
            this.serializer = serializer;
            this.metadataProvider = metadataProvider;
            this.topicProvider = topicProvider;

            var kafkaOptions = new KafkaOptions(KafkaSettings.Current.KafkaUris) {
                Log = KafkaLog.Instance
            };
            this.producer = new Lazy<Producer>(() => new Producer(new BrokerRouter(kafkaOptions)), false);

            this.consumers = new Dictionary<string, Consumer>();
            this.metadatas = new Dictionary<string, ConcurrentDictionary<string, MessageMetadata>>();

            foreach (var topic in KafkaSettings.Current.Topics) {
                consumers[topic] = new Consumer(new ConsumerOptions(topic, new BrokerRouter(kafkaOptions)) {
                    Log = KafkaLog.Instance
                });
                metadatas[topic] = new ConcurrentDictionary<string, MessageMetadata>();
            }
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
            }
        }

        //public void EnsureProducerTopic(params string[] topics)
        //{
        //    foreach (var topic in topics) {
        //        int count = -1;
        //        while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
        //            try {
        //                Topic result = producer.Value.GetTopic(topic);
        //                if (result.ErrorCode == (short)ErrorResponseCode.NoError) {
        //                    break;
        //                }

        //                Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
        //            }
        //            catch (Exception) {
        //            }
        //        }
        //    }
        //}

        //public void EnsureConsumerTopic(params string[] topics)
        //{
        //    foreach (var topic in topics) {
        //        int count = -1;
        //        var consumer = new Consumer(new ConsumerOptions(topic, router));
        //        while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
        //            try {
        //                Topic result = consumer.GetTopic(topic);
        //                if (result.ErrorCode == (short)ErrorResponseCode.NoError) {
        //                    consumers.TryAdd(topic, new KafkaConsumer(consumer, topic));
        //                    break;
        //                }

        //                if (LogManager.Default.IsDebugEnabled)
        //                    LogManager.Default.DebugFormat("get the topic('{0}') of status is {1}", topic, (ErrorResponseCode)result.ErrorCode);

        //                Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
        //            }
        //            catch (Exception) {
        //            }
        //        }
        //    }
        //}

        public void Push(IEnumerable messages)
        {
            Dictionary<string, IList<Message>> dict = new Dictionary<string, IList<Message>>();

            for (IEnumerator item = messages.GetEnumerator(); item.MoveNext(); ) {
                var topic = topicProvider.GetTopic(item.Current);

                var message = this.Serialize(item.Current);
                dict.GetOrAdd(topic, () => new List<Message>()).Add(message);
                //IList<Message> list;
                //if (!dict.TryGetValue(topic, out list)) {
                //    list = new List<Message>();
                //    dict.Add(topic, list);
                //}
                //list.Add(Serialize(item.Current));
            }

            var tasks = dict.Select(item => producer.Value.SendMessageAsync(item.Key, item.Value)).ToArray();
            Task.WaitAll(tasks);
        }

        public void Pull(string topic, Action<object> action)
        {
            var consumer = consumers[topic];
            var metadata = metadatas[topic];

            foreach (var message in consumer.Consume()) {
                object item;
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
    }
}
