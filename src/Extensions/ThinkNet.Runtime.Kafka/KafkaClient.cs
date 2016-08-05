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
    public class KafkaClient
    {
        public readonly static KafkaClient Instance = new KafkaClient();


        private readonly ITextSerializer serializer;
        private readonly IMetadataProvider metadataProvider;
        private readonly ITopicProvider topicProvider;

        private readonly IBrokerRouter router;
        private readonly Lazy<Producer> producer;
        private readonly ConcurrentDictionary<string, Consumer> consumers;

        private KafkaClient()
            : this(KafkaSettings.Current.KafkaUris)
        { }

        public KafkaClient(params string[] kafkaUrls)
        {
            this.serializer = ServiceLocator.Current.GetInstance<ITextSerializer>();
            this.metadataProvider = ServiceLocator.Current.GetInstance<IMetadataProvider>();
            this.topicProvider = ServiceLocator.Current.GetInstance<ITopicProvider>();


            var uris = kafkaUrls.Select(url => new Uri(url)).ToArray();
            var option = new KafkaOptions(uris);
            this.router = new BrokerRouter(option);

            this.producer = new Lazy<Producer>(() => new Producer(router), true);
            this.consumers = new ConcurrentDictionary<string, Consumer>();
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
                var consumer = consumers.GetOrAdd(topic, key => new Consumer(new ConsumerOptions(key, router)));
                while (count++ < KafkaSettings.Current.EnsureTopicRetrycount) {
                    try {
                        Topic result = consumer.GetTopic(topic);
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


        public void Push(IEnumerable messages)
        {
            Dictionary<string, IList<Message>> dict = new Dictionary<string, IList<Message>>();

            for (IEnumerator item = messages.GetEnumerator(); item.MoveNext(); ) {
                var topic = topicProvider.GetTopic(item.Current);

                //dict.GetOrAdd(topic)
                IList<Message> list;
                if (!dict.TryGetValue(topic, out list)) {
                    list = new List<Message>();
                    dict.Add(topic, list);
                }
                list.Add(Serialize(item.Current));
            }

            var tasks = dict.Select(item => producer.Value.SendMessageAsync(item.Key, item.Value)).ToArray();
            Task.WaitAll(tasks);
        }

        public IEnumerable Pull(string topic)
        {
            return consumers[topic].Consume().Select(Deserialize).ToArray();
        }

        private object Deserialize(Message message)
        {
            var serialized = message.Value.ToUtf8String();
            var kafkaMsg = serializer.Deserialize<KafkaMessage>(serialized);

            return Deserialize(kafkaMsg);
        }

        private object Deserialize(KafkaMessage message)
        {
            var typeFullName = string.Format("{0}.{1}, {2}",
                message.MetadataInfo[StandardMetadata.Namespace], 
                message.MetadataInfo[StandardMetadata.TypeName],
                message.MetadataInfo[StandardMetadata.AssemblyName]);
            var type = Type.GetType(typeFullName);

            return serializer.Deserialize(message.Body, type);
        }

        private Message Serialize(object message)
        {
            var kafkaMsg = new KafkaMessage {
                MetadataInfo = metadataProvider.GetMetadata(message),
                Body = serializer.Serialize(message)
            };

            return new Message(serializer.Serialize(kafkaMsg));
        }

        class KafkaMessage
        {
            /// <summary>
            /// 元数据信息
            /// </summary>
            public IDictionary<string, string> MetadataInfo { get; set; }

            public string Body { get; set; }
        }
    }
}
