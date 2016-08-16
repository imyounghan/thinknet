using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;


namespace ThinkNet.Messaging
{
    public abstract class KafkaBus : AbstractBus
    {
        private readonly ISerializer serializer;
        private readonly IMetadataProvider metadataProvider;
        private readonly ITopicProvider topicProvider;

        private readonly Lazy<KafkaNet.Producer> producer;
        protected KafkaBus(ISerializer serializer, IMetadataProvider metadataProvider, ITopicProvider topicProvider)
        {
            this.serializer = serializer;
            this.metadataProvider = metadataProvider;
            this.topicProvider = topicProvider;

            this.producer = new Lazy<KafkaNet.Producer>(CreateProducer, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private KafkaNet.Producer CreateProducer()
        {
            var options = new KafkaNet.Model.KafkaOptions(KafkaSettings.Current.KafkaUris) {
                Log = KafkaLog.Instance
            };
            var router = new KafkaNet.BrokerRouter(options);

            return new KafkaNet.Producer(router);
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();
            if (disposing && producer.IsValueCreated) {
                producer.Value.Dispose();
            }
        }

        protected void Push(IEnumerable messages)
        {
            var dict = new Dictionary<string, IList<KafkaNet.Protocol.Message>>();

            for (IEnumerator item = messages.GetEnumerator(); item.MoveNext(); ) {
                var topic = topicProvider.GetTopic(item.Current);

                var message = this.Serialize(item.Current);
                dict.GetOrAdd(topic, () => new List<KafkaNet.Protocol.Message>()).Add(message);
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

        private KafkaNet.Protocol.Message Serialize(object message)
        {
            var metadata = metadataProvider.GetMetadata(message);
            metadata["Playload"] = serializer.Serialize(message);

            return new KafkaNet.Protocol.Message(serializer.Serialize(metadata));
        }
    }
}
