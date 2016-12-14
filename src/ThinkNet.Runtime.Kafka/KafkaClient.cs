using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Helper;
using Kafka.Client.Messages;
using Kafka.Client.Producers;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;

namespace ThinkNet.Runtime.Kafka
{
    public class KafkaClient : DisposableObject
    {
        private readonly Lazy<Producer> _producer;
        private readonly ConcurrentDictionary<string, ZookeeperConsumerConnector> _consumers;

        
        private readonly ZooKeeperConfiguration _zooKeeperConfiguration;
        private readonly ITopicProvider _topicProvider;

        public KafkaClient(string zkConnectionString, ITopicProvider topicProvider)
            : this(zkConnectionString)
        {
            this._topicProvider = topicProvider;
            this._producer = new Lazy<Producer>(CreateProducer, LazyThreadSafetyMode.ExecutionAndPublication);
            this._consumers = new ConcurrentDictionary<string, ZookeeperConsumerConnector>();
        }

        internal KafkaClient(string zkConnectionString)
        {            
            this._zooKeeperConfiguration = new ZooKeeperConfiguration(zkConnectionString, 3000, 4000, 8000);
        }

        private Producer CreateProducer()
        {
            var producerConfiguration = new ProducerConfiguration(new List<BrokerConfiguration>()) {
                AckTimeout = 30000,
                RequiredAcks = -1,
                ZooKeeper = _zooKeeperConfiguration
            };

            return new Producer(producerConfiguration);
        }

        private ZookeeperConsumerConnector CreateConsumer(string consumerId)
        {
            var consumerConfiguration = new ConsumerConfiguration {
                AutoCommit = false,
                GroupId = "thinknet",
                ConsumerId = consumerId.AfterContact("_Consumer"),
                //BufferSize = ConsumerConfiguration.DefaultBufferSize,
                //MaxFetchBufferLength = ConsumerConfiguration.DefaultMaxFetchBufferLength,
                //FetchSize = ConsumerConfiguration.DefaultFetchSize,
                //AutoOffsetReset = OffsetRequest.SmallestTime,
                ZooKeeper = _zooKeeperConfiguration,
            };

            return new ZookeeperConsumerConnector(consumerConfiguration, true);
        }


        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();
            if(disposing) {
                if(_producer != null && _producer.IsValueCreated)
                    _producer.Value.Dispose();

                if(_consumers != null)
                    _consumers.Values.ForEach(item => item.Dispose());
            }
        }

        public void CreateTopicIfNotExists(string topic)
        {
            if(!TopicExsits(topic)) {
                CreateTopic(topic);
            }
        }

        bool TopicExsits(string topic)
        {
            var managerConfig = new KafkaSimpleManagerConfiguration() {
                FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                BufferSize = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                Zookeeper = _zooKeeperConfiguration.ZkConnect
            };
            using(var kafkaManager = new KafkaSimpleManager<string, Message>(managerConfig)) {
                try {
                    var allPartitions = kafkaManager.GetTopicPartitionsFromZK(topic);
                    return allPartitions.Count > 0;
                }
                catch(Exception ex) {
                    if(LogManager.Default.IsErrorEnabled)
                        LogManager.Default.Error(ex, "Get topic {0} failed", topic);
                    return false;
                }
            }
        }

        void CreateTopic(string topic)
        {
            using(var producer = CreateProducer()) {
                try {
                    var data = new ProducerData<string, Message>(topic, string.Empty, new Message(new byte[0]));
                    producer.Send(data);
                }
                catch(Exception ex) {
                    if(LogManager.Default.IsErrorEnabled)
                        LogManager.Default.Error(ex, "Create topic {0} failed", topic);
                }
            }
        }

        private Task PushToKafka(IEnumerable<ProducerData<string, Message>> producerDatas)
        {
            if(LogManager.Default.IsDebugEnabled) {
                var topics = producerDatas.Select(item => item.Topic).Distinct().ToArray();
                LogManager.Default.DebugFormat("Ready to push messages to kafka on topic('{0}').", 
                    string.Join("','", topics));
            }

            return Task.Factory.StartNew(() => _producer.Value.Send(producerDatas));
        }

        private string GetTopic<T>(T element)
        {
            return _topicProvider.GetTopic(element);
        }

        public Task Push<T>(T element, Func<T, byte[]> serializer)
        {
            return this.Push(new T[] { element }, serializer);
        }
        public Task Push<T>(IEnumerable<T> elements, Func<T, byte[]> serializer)
        {
            var producerDatas = new List<ProducerData<string, Message>>();
            foreach(var element in elements) {
                var message = new Message(serializer(element));
                var topic = this.GetTopic(element);
                var key = element.GetType().FullName;
                producerDatas.Add(new ProducerData<string, Message>(topic, key, message));
            }

            return this.PushToKafka(producerDatas);
        }


        public void CommitOffset(string topic, OffsetPosition offsetPosition)
        {
            _consumers[topic].CommitOffset(topic,
                offsetPosition.PartitionId,
                offsetPosition.Offset, 
                false);
        }

        
        public void Consume<T>(CancellationToken cancellationToken,
            string topic, 
            Func<byte[], Type, T> deserializer, 
            Action<T, string, OffsetPosition> consumer)
        {
            var topicMap = new Dictionary<string, int>() {
                { topic, 1 }
            };
            var streams = _consumers.GetOrAdd(topic, CreateConsumer).CreateMessageStreams(topicMap, new DefaultDecoder());

            var KafkaMessageStream = streams[topic][0];
            var type = _topicProvider.GetType(topic);

            foreach(Message message in KafkaMessageStream.GetCancellable(cancellationToken)) {
                try {
                    if(LogManager.Default.IsDebugEnabled) {
                        LogManager.Default.DebugFormat("Pull a message from kafka on topic('{0}'). offset:{1}, partition:{2}.", topic, message.Offset, message.PartitionId);
                    }
                    var result = deserializer(message.Payload, type);
                    consumer(result, topic, new OffsetPosition(message.PartitionId.Value, message.Offset));
                }
                catch(OperationCanceledException) {
                    break;
                }
                catch(ThreadAbortException) {
                    break;
                }
                catch(Exception ex) {
                    if(LogManager.Default.IsErrorEnabled)
                        LogManager.Default.Error(ex.GetBaseException().Message, ex);
                }
            }
        }        
    }
}
