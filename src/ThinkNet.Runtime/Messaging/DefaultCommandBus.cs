using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    internal class DefaultCommandBus : AbstractBus, ICommandBus
    {
        private readonly BlockingCollection<ICommand> queue;
        private readonly Worker worker;

        public DefaultCommandBus()
        {
            this.queue = new BlockingCollection<ICommand>();
            this.worker = WorkerFactory.Create<ICommand>(queue.Take, Transform);
        }

        protected override void Initialize(IEnumerable<Type> types)
        {
            worker.Start();
        }

        protected override bool MatchType(Type type)
        {
            return TypeHelper.IsCommand(type);
        }

        public void Send(ICommand command)
        {
            this.Send(new[] { command });
        }

        public void Send(IEnumerable<ICommand> commands)
        {
            if (commands.IsEmpty())
                return;

            if (queue.Count + commands.Count() >= ConfigurationSetting.Current.QueueCapacity * 5)
                throw new Exception("server is busy.");

            commands.ForEach(queue.Add);
        }

        private void Transform(ICommand command)
        {
            var item = new Envelope<ICommand>(command) {
                CorrelationId = command.Id
            };

            EnvelopeBuffer<ICommand>.Instance.Enqueue(item);
            item.WaitTime = DateTime.UtcNow - command.CreatedTime;
        }
    }
}
