using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using KafkaNet.Protocol;
using ThinkNet.Configurations;

namespace ThinkNet.Infrastructure
{
    public class KafkaClient : DisposableObject
    {

        //public readonly static KafkaClient Instance = new KafkaClient();


        private readonly ISerializer serializer;
        private readonly ITopicProvider _topicProvider;

        private readonly IBrokerRouter router;
        private readonly Lazy<Producer> producer;
        private readonly ConcurrentDictionary<string, Consumer> consumers;

        private readonly Dictionary<string, ConcurrentDictionary<string, MessageMetadata>> metadatas;
        private readonly Dictionary<string, ConcurrentDictionary<int, long>> lasted;
        private readonly Dictionary<string, OffsetPosition[]> final;


        private KafkaClient()
            : this(KafkaSettings.Current.KafkaUris)
        { }

        public KafkaClient(params Uri[] kafkaUris)
        {
            var option = new KafkaOptions(kafkaUris);
            this.router = new BrokerRouter(option);

            this.producer = new Lazy<Producer>(() => new Producer(router), true);
            this.consumers = new ConcurrentDictionary<string, Consumer>();
            //this.metadatas = new ConcurrentDictionary<string, MessageMetadata>();
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();
            if(disposing) {
                if(producer.IsValueCreated)
                    producer.Value.Dispose();

                foreach(var kvp in consumers) {
                    kvp.Value.Dispose();
                }
                consumers.Clear();
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

        private Task PushToKafka(KeyValuePair<string, Message[]> item)
        {
            return producer.Value.SendMessageAsync(item.Key, item.Value).ContinueWith(task => {
                if(task.IsFaulted) {
                    if(LogManager.Default.IsErrorEnabled) {
                        LogManager.Default.Error("send to kafka encountered error.", task.Exception);
                    }
                }
            });
        }

        public Task Push<T>(IEnumerable<T> elements, Func<T, Message> serializer)
        {
            var topic = _topicProvider.GetTopic(typeof(T));

             var messages = elements.AsParallel().Select(serializer).ToArray();

            return producer.Value.SendMessageAsync(topic, messages).ContinueWith(task => {
                if(task.IsFaulted) {
                    if(LogManager.Default.IsErrorEnabled) {
                        LogManager.Default.Error("send to kafka encountered error.", task.Exception);
                    }
                }
            });
        }
        //public void Push<T>(IEnumerable<T> messages, Func<T, KeyValuePair<string, Message>> serializer)
        //{
        //    var topic = _topicProvider.GetTopic(typeof(T));


        //    var tasks = messages.AsParallel()
        //        .Select(serializer)
        //        .GroupBy(item => item.Key)
        //        .ToDictionary(item => item.Key, item => item.Select(p => p.Value).ToArray())
        //        .Select(PushToKafka)
        //        .ToArray();

        //    Task.WaitAll(tasks);
        //}

        private void Add(string topic, string correlationId, MessageMetadata meta)
        {
            lasted[topic].AddOrUpdate(meta.PartitionId, meta.Offset, (key, value) => meta.Offset);
            if(!string.IsNullOrEmpty(correlationId)) {
                metadatas[topic].TryAdd(correlationId, meta);
            }
        }

        public void Remove(string topic, string correlationId)
        {
            if(!string.IsNullOrEmpty(correlationId)) {
                metadatas[topic].Remove(correlationId);
            }
        }

        public IDictionary<string, OffsetPosition[]> Get()
        {
            if(metadatas.All(p => p.Value.Count == 0) && lasted.All(p => p.Value.Count == 0)) {
                final.Clear();
                return final;
            }

            foreach(var topic in KafkaSettings.Current.Topics) {
                OffsetPosition[] positions;
                if(metadatas[topic].Count == 0) {
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

        public IEnumerable<T> Pull<T>(string topic, Func<string, Type, T> deserializer, Func<T, string> getKey)
        {
            var type = _topicProvider.GetType(topic);

            return consumers[topic].Consume()
                .Select(message => {
                    var serialized = message.Value.ToUtf8String();
                    var result = deserializer(serialized, type);
                    this.Add(topic, getKey(result), message.Meta);
                    return result;
                }).ToArray();
        }

        //private object Deserialize(Message message)
        //{
        //    var serialized = message.Value.ToUtf8String();
        //    var metadata = serializer.Deserialize<IDictionary<string, string>>(serialized, true);

        //    var typeFullName = string.Format("{0}.{1}, {2}",
        //        metadata[StandardMetadata.Namespace],
        //        metadata[StandardMetadata.TypeName],
        //        metadata[StandardMetadata.AssemblyName]);
        //    var type = Type.GetType(typeFullName);

        //    return serializer.Deserialize(metadata["Playload"], type);
        //}

        //private Message Serialize(object message)
        //{
        //    var metadata = metadataProvider.GetMetadata(message);
        //    metadata["Playload"] = serializer.Serialize(message);

        //    return new Message(serializer.Serialize(metadata, true));
        //}

        //public void ConsumerComplete(string topic, string messageId)
        //{
        //    consumers[topic].Metadatas.Remove(messageId);
        //}
    }
}
