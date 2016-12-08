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
        {
            this._topicProvider = topicProvider;
            this._zooKeeperConfiguration = new ZooKeeperConfiguration(zkConnectionString, 3000, 4000, 8000);

            this._producer = new Lazy<Producer>(CreateProducer, LazyThreadSafetyMode.ExecutionAndPublication);
            this._consumers = new ConcurrentDictionary<string, ZookeeperConsumerConnector>();
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

        private ZookeeperConsumerConnector CreateConsumer()
        {
            var consumerConfiguration = new ConsumerConfiguration {
                //BackOffIncrement = 30,
                AutoCommit = false,
                GroupId = "thinknet",
               // ConsumerId = consumerId,
                BufferSize = ConsumerConfiguration.DefaultBufferSize,
                MaxFetchBufferLength = ConsumerConfiguration.DefaultMaxFetchBufferLength,
                FetchSize = ConsumerConfiguration.DefaultFetchSize,
                AutoOffsetReset = OffsetRequest.LargestTime,
                ZooKeeper = _zooKeeperConfiguration,
                ShutdownTimeout = 100
            };

            return new ZookeeperConsumerConnector(consumerConfiguration, true);
        }


        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();
            if(disposing) {
                if(_producer.IsValueCreated)
                    _producer.Value.Dispose();

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
                catch(Exception) {
                    return false;
                }
            }
        }

        void CreateTopic(string topic)
        {
            try {
                var data = new ProducerData<string, Message>(topic, string.Empty, new Message(new byte[0]));
                _producer.Value.Send(data);
            }
            catch(Exception ex) {
                if(LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(ex, "Create topic {0} failed", topic);
            }
        }

        private Task PushToKafka(IEnumerable<ProducerData<string, Message>> producerDatas)
        {
            if(LogManager.Default.IsDebugEnabled) {
                var topics = producerDatas.Select(item => item.Topic).ToArray();
                LogManager.Default.DebugFormat("ready to send a message to kafka in topic('{0}').", 
                    string.Join("','", topics));
            }

            return Task.Factory.StartNew(() => _producer.Value.Send(producerDatas));
        }

        private string GetTopic<T>(T element)
        {
            return _topicProvider.GetTopic(element);
        }

        //public Task Push<T>(string topic, IEnumerable<T> elements, Func<T, byte[]> serializer)
        //{
        //    var producerDatas = elements
        //        .Select(item => {
        //            var message = new Message(serializer(item));
        //            return new ProducerData<string, Message>(topic, message);
        //        }).ToArray();

        //    return this.PushToKafka(producerDatas);
        //}
        public Task Push<T>(IEnumerable<T> elements, Func<T, byte[]> serializer)
        {
            var producerDatas = elements.GroupBy(GetTopic)
                .Select(group => {
                    var messages = group.Select(item => new Message(serializer(item))).ToArray();
                    return new ProducerData<string, Message>(group.Key, messages);
                }).ToArray();

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
            var topicDic = new Dictionary<string, int>() {
                { topic, 1 }
            };
            var streams = _consumers.GetOrAdd(topic, CreateConsumer).CreateMessageStreams(topicDic, new DefaultDecoder());

            var KafkaMessageStream = streams[topic][0];
            var type = _topicProvider.GetType(topic);

            foreach(Message message in KafkaMessageStream.GetCancellable(cancellationToken)) {
                try {
                    var result = deserializer(message.Payload, type);
                    consumer(result, topic, new OffsetPosition(message.PartitionId.Value, message.Offset));
                }
                catch(OperationCanceledException) {
                    break;
                }
                catch(ThreadAbortException) {
                    if(LogManager.Default.IsErrorEnabled) {
                        LogManager.Default.Error("Kafka Consume ");
                    }
                    break;
                }
                catch(Exception ex) {
                    //this.CommitOffset(offset);

                    if(LogManager.Default.IsErrorEnabled)
                        LogManager.Default.Error(ex.GetBaseException().Message, ex);
                }
            }
        }        
    }
}
