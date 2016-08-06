using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using KafkaNet.Protocol;
using Microsoft.Practices.ServiceLocation;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Runtime
{
    public class KafkaClient : DisposableObject
    {
        class KafkaConsumer : IDisposable
        {

            private readonly Consumer _consumer;

            public KafkaConsumer(Consumer consumer, string topic)
            {
                this._consumer = consumer;
                this.Topic = topic;
                this.Metadatas = new ConcurrentDictionary<string, MessageMetadata>();
            }

            public string Topic { get; private set; }

            public IEnumerable Consume()
            {
                return _consumer.Consume().Aggregate(new ArrayList(), (list, item) => {
                    list.Add(Deserialize(item));
                    return list;
                });
            }

            private object Deserialize(Message message)
            {
                var serialized = message.Value.ToUtf8String();
                var metadata = serializer.Deserialize<IDictionary<string, string>>(serialized);

                var typeFullName = string.Format("{0}.{1}, {2}",
                    metadata[StandardMetadata.Namespace],
                    metadata[StandardMetadata.TypeName],
                    metadata[StandardMetadata.AssemblyName]);
                var type = Type.GetType(typeFullName);

                Metadatas.TryAdd(metadata[StandardMetadata.UniqueId], message.Meta);

                return serializer.Deserialize(metadata["Playload"], type);
            }


            public ConcurrentDictionary<string, MessageMetadata> Metadatas { get; private set; }

            #region IDisposable 成员

            public void Dispose()
            {
                _consumer.Dispose();
            }

            #endregion
        }


        public readonly static KafkaClient Instance = new KafkaClient();


        private readonly static ISerializer serializer;
        private readonly static IMetadataProvider metadataProvider;
        private readonly static ITopicProvider topicProvider;

        private readonly IBrokerRouter router;
        private readonly Lazy<Producer> producer;
        private readonly ConcurrentDictionary<string, KafkaConsumer> consumers;
        private readonly ConcurrentDictionary<string, MessageMetadata> metadatas;

        static KafkaClient()
        {
            serializer = ServiceLocator.Current.GetInstance<ISerializer>();
            metadataProvider = ServiceLocator.Current.GetInstance<IMetadataProvider>();
            topicProvider = ServiceLocator.Current.GetInstance<ITopicProvider>();
        }

        private KafkaClient()
            : this(KafkaSettings.Current.KafkaUris)
        { }

        public KafkaClient(params Uri[] kafkaUris)
        {
            var option = new KafkaOptions(kafkaUris);
            this.router = new BrokerRouter(option);

            this.producer = new Lazy<Producer>(() => new Producer(router), true);
            this.consumers = new ConcurrentDictionary<string, KafkaConsumer>();
            this.metadatas = new ConcurrentDictionary<string, MessageMetadata>();
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

        public void EnsureProducerTopic(params string[] topics)
        {
            foreach (var topic in topics) {
                int count = -1;
                while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
                    try {
                        Topic result = producer.Value.GetTopic(topic);
                        if (result.ErrorCode == (short)ErrorResponseCode.NoError) {
                            break;
                        }

                        Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
                    }
                    catch (Exception) {
                    }
                }
            }
        }

        public void EnsureConsumerTopic(params string[] topics)
        {
            foreach (var topic in topics) {
                int count = -1;
                var consumer = new Consumer(new ConsumerOptions(topic, router));
                while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
                    try {
                        Topic result = consumer.GetTopic(topic);
                        if (result.ErrorCode == (short)ErrorResponseCode.NoError) {
                            consumers.TryAdd(topic, new KafkaConsumer(consumer, topic));
                            break;
                        }

                        if (LogManager.Default.IsDebugEnabled)
                            LogManager.Default.DebugFormat("get the topic('{0}') of status is {1}", topic, (ErrorResponseCode)result.ErrorCode);

                        Thread.Sleep(KafkaSettings.Current.EnsureTopicRetryInterval);
                    }
                    catch (Exception) {
                    }
                }
            }
        }


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

        public IEnumerable Pull(string topic)
        {
            return consumers[topic].Consume();
        }

        private object Deserialize(Message message)
        {
            var serialized = message.Value.ToUtf8String();
            var metadata = serializer.Deserialize<IDictionary<string, string>>(serialized);

            var typeFullName = string.Format("{0}.{1}, {2}",
                metadata[StandardMetadata.Namespace],
                metadata[StandardMetadata.TypeName],
                metadata[StandardMetadata.AssemblyName]);
            var type = Type.GetType(typeFullName);

            return serializer.Deserialize(metadata["Playload"], type);
        }

        private Message Serialize(object message)
        {
            var metadata = metadataProvider.GetMetadata(message);
            metadata["Playload"] = serializer.Serialize(message);

            return new Message(serializer.Serialize(metadata));
        }

        public void ConsumerComplete(string topic, string messageId)
        {
            consumers[topic].Metadatas.Remove(messageId);
        }
    }
}
