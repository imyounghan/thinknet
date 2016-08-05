using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class KafkaProcessor : DisposableObject, IProcessor, IInitializer
    {
        private readonly Worker[] workers;

        private readonly object lockObject;
        private bool started;

        public KafkaProcessor()
        {
            this.workers = KafkaSettings.Current.ConsumerTopics.Select(CreateWorker).ToArray();
            this.lockObject = new object();
        }

        /// <summary>
        /// Disposes the resources used by the processor.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if (disposing) {
                this.Stop();
            }
        }

        private Worker CreateWorker(string topic)
        {
            return WorkerFactory.Create<IEnumerable>(() => KafkaClient.Instance.Pull(topic), Distribute);
        }

        private void Distribute(IEnumerable messages)
        {
            for (IEnumerator item = messages.GetEnumerator(); item.MoveNext(); ) {
                var command  = item.Current as ICommand;
                if (command != null) {
                    var envelope = Transfer(command);
                    EnvelopeBuffer<ICommand>.Instance.Enqueue(envelope);
                    envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                    continue;
                }

                var stream  = item.Current as EventStream;
                if (stream != null) {
                    var envelope = Transfer(stream);
                    EnvelopeBuffer<EventStream>.Instance.Enqueue(envelope);
                    envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                    continue;
                }

                var @event  = item.Current as IEvent;
                if (@event != null) {
                    var envelope = Transfer(@event);
                    EnvelopeBuffer<IEvent>.Instance.Enqueue(envelope);
                    envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                    continue;
                }

                var notification = item.Current as CommandReply;
                if (notification != null) {
                    var envelope = Transfer(notification);
                    EnvelopeBuffer<CommandReply>.Instance.Enqueue(envelope);
                    envelope.WaitTime = DateTime.UtcNow - command.CreatedTime;
                    continue;
                }
            }
        }
        private Envelope<T> Transfer<T>(T message)
            where T : IMessage
        {
            return new Envelope<T>(message) {
                CorrelationId = message.Id
            };
        }

        #region IProcessor 成员

        public void Start()
        {
            ThrowIfDisposed();
            lock (this.lockObject) {
                if (!this.started) {
                    workers.ForEach(worker => worker.Start());
                    this.started = true;
                }
            }
        }

        public void Stop()
        {
            lock (this.lockObject) {
                if (this.started) {
                    workers.ForEach(worker => worker.Stop());
                    this.started = false;
                }
            }
        }

        #endregion

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            KafkaClient.Instance.EnsureProducerTopic(KafkaSettings.Current.ProducerTopics);
            KafkaClient.Instance.EnsureConsumerTopic(KafkaSettings.Current.ConsumerTopics);
            this.Start();
        }

        #endregion
    }
}
