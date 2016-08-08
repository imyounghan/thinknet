using System;
using System.Collections.Generic;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class KafkaProcessor : Processor, IInitializer
    {
        private readonly KafkaClient _kafkaClient;

        public KafkaProcessor(KafkaClient kafkaClient)
        {
            this._kafkaClient = kafkaClient;
        }

        
        private void Distribute(object message)
        {
            var command  = message as ICommand;
            if (command != null) {
                var envelope = Transform(command);
                EnvelopeBuffer<ICommand>.Instance.Enqueue(envelope);
                envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                return;
            }

            var stream  = message as EventStream;
            if (stream != null) {
                var envelope = Transform(stream);
                EnvelopeBuffer<EventStream>.Instance.Enqueue(envelope);
                envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                return;
            }

            var @event  = message as IEvent;
            if (@event != null) {
                var envelope = Transform(@event);
                EnvelopeBuffer<IEvent>.Instance.Enqueue(envelope);
                envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                return;
            }

            var notification = message as CommandReply;
            if (notification != null) {
                var envelope = Transform(notification);
                EnvelopeBuffer<CommandReply>.Instance.Enqueue(envelope);
                envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                return;
            }
        }
        private Envelope<T> Transform<T>(T message)
            where T : IMessage
        {
            return new Envelope<T>(message) {
                CorrelationId = message.Id
            };
        }


        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            KafkaSettings.Current.Topics.ForEach(topic => {
                base.BuildWorker(() => _kafkaClient.Pull(topic, Distribute));
            });
            //var topics = KafkaSettings.Current.ProducerTopics.Union(KafkaSettings.Current.ConsumerTopics).Distinct().ToArray();
            //KafkaClient.Instance.EnsureTopics(topics);
            //KafkaClient.Instance.EnsureProducerTopic(KafkaSettings.Current.ProducerTopics);
            //KafkaClient.Instance.EnsureConsumerTopic(KafkaSettings.Current.ConsumerTopics);
        }

        #endregion
    }
}
