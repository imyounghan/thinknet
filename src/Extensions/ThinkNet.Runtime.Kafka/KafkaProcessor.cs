using System;
using System.Collections;
using System.Collections.Generic;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class KafkaProcessor : Processor, IInitializer
    {
        public KafkaProcessor()
        {
            KafkaSettings.Current.ConsumerTopics.ForEach(topic => {
                base.BuildWorker<IEnumerable>(() => KafkaClient.Instance.Pull(topic), Distribute);
            });
        }

        private void Distribute(IEnumerable messages)
        {
            for (IEnumerator item = messages.GetEnumerator(); item.MoveNext(); ) {
                var command  = item.Current as ICommand;
                if (command != null) {
                    var envelope = Transform(command);
                    EnvelopeBuffer<ICommand>.Instance.Enqueue(envelope);
                    envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                    continue;
                }

                var stream  = item.Current as EventStream;
                if (stream != null) {
                    var envelope = Transform(stream);
                    EnvelopeBuffer<EventStream>.Instance.Enqueue(envelope);
                    envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                    continue;
                }

                var @event  = item.Current as IEvent;
                if (@event != null) {
                    var envelope = Transform(@event);
                    EnvelopeBuffer<IEvent>.Instance.Enqueue(envelope);
                    envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                    continue;
                }

                var notification = item.Current as CommandReply;
                if (notification != null) {
                    var envelope = Transform(notification);
                    EnvelopeBuffer<CommandReply>.Instance.Enqueue(envelope);
                    envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                    continue;
                }
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
            KafkaClient.Instance.EnsureProducerTopic(KafkaSettings.Current.ProducerTopics);
            KafkaClient.Instance.EnsureConsumerTopic(KafkaSettings.Current.ConsumerTopics);
        }

        #endregion
    }
}
