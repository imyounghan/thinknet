using System;
using System.Collections.Generic;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class KafkaProcessor : Processor
    {
        public KafkaProcessor(KafkaClient kafkaClient)
        {
            foreach (var topic in KafkaSettings.Current.Topics) {
                base.BuildWorker(() => kafkaClient.Pull(topic, Distribute));
            }
        }

        
        private void Distribute(object message)
        {
            var command  = message as ICommand;
            if (command != null) {
                var envelope = Transform(command);
                EnvelopeBuffer<ICommand>.Instance.Enqueue(envelope);
                return;
            }

            var stream  = message as EventStream;
            if (stream != null) {
                var envelope = Transform(stream);
                EnvelopeBuffer<EventStream>.Instance.Enqueue(envelope);
                return;
            }

            var @event  = message as IEvent;
            if (@event != null) {
                var envelope = Transform(@event);
                EnvelopeBuffer<IEvent>.Instance.Enqueue(envelope);
                return;
            }

            var notification = message as CommandReply;
            if (notification != null) {
                var envelope = Transform(notification);
                EnvelopeBuffer<CommandReply>.Instance.Enqueue(envelope);
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
    }
}
