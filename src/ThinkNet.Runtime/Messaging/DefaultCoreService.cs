using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Processing;

namespace ThinkNet.Messaging
{
    internal class DefaultCoreService : CommandService, ICommandNotification, ICommandBus, IEventBus, IInitializer
    {
        private readonly IRoutingKeyProvider _routingKeyProvider;
        private readonly IHandlerProvider _handlerProvider;
        private readonly BlockingCollection<IMessage>[] _brokers;

        private readonly Dictionary<string, IMessageExecutor> executorDict;
        //private readonly object lockObject;

        private CancellationTokenSource cancellationSource;

        public DefaultCoreService(IRoutingKeyProvider routingKeyProvider,
            IHandlerRecordStore handlerStore,
            IEventPublishedVersionStore eventPublishedVersionStore,
            IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ISnapshotPolicy snapshotPolicy,
            ICache cache,
            ISerializer serializer)
        {
            this._routingKeyProvider = routingKeyProvider;
            //this.lockObject = new object();

            var queueCount = ConfigurationSetting.Current.QueueCount;
            this._brokers = new BlockingCollection<IMessage>[queueCount];
            for(int i = 0; i < queueCount; i++) {
                this._brokers[i] = new BlockingCollection<IMessage>();
            }

            var repository = new EventSourcedRepository(eventStore, snapshotStore, snapshotPolicy, cache, this, serializer);
            this._handlerProvider = new DefaultHandlerProvider(repository, this);
            this.executorDict = new Dictionary<string, IMessageExecutor>() {
                { "Command", new CommandExecutor(this, _handlerProvider) },
                { "Event", new EventExecutor(handlerStore, _handlerProvider) },
                { "EventStream", new SynchronousExecutor(this, _handlerProvider, this, eventPublishedVersionStore, serializer) }
            };
        }

        public override void Send(ICommand command)
        {
            this.Distribute(command);
        }
        #region IMessageNotification 成员

        public void NotifyCompleted(string commandId, Exception exception = null)
        {
            this.NotifyCommandCompleted(commandId,
                exception == null ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyHandled(string commandId, Exception exception = null)
        {
            this.NotifyCommandExecuted(commandId,
                exception == null ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyUnchanged(string commandId)
        {
            this.NotifyCommandCompleted(commandId, CommandStatus.NothingChanged, null);
        }

        #endregion


        private BlockingCollection<IMessage> GetBroker(string routingKey)
        {
            if(_brokers.Length == 1) {
                return _brokers[0];
            }

            if(string.IsNullOrWhiteSpace(routingKey)) {
                return _brokers.OrderBy(broker => broker.Count).First();
            }

            var index = Math.Abs(routingKey.GetHashCode() % _brokers.Length);
            return _brokers[index];
        }

        private void Distribute(IMessage message)
        {
            var routingKey = _routingKeyProvider.GetRoutingKey(message);

            GetBroker(routingKey).Add(message);
        }

        #region IEventBus 成员
        public void Publish(IEnumerable<IEvent> events)
        {
            events.ForEach(this.Distribute);
        }

        public void Publish(IEvent @event)
        {
            this.Distribute(@event);
        }

        #endregion

        #region ICommandBus 成员
        public void Send(IEnumerable<ICommand> commands)
        {
            commands.ForEach(this.Distribute);
        }

        //public void Send(ICommand command)
        //{
        //    this.Distribute(command);
        //}
        #endregion


        #region
        private void Consume(object state)
        {
            var broker = state as BlockingCollection<IMessage>;
            broker.NotNull("broker");

            while(!cancellationSource.Token.IsCancellationRequested) {
                var message = broker.Take();
                IMessageExecutor executor;
                var kind = this.GetGroup(message);
                if(!string.IsNullOrEmpty(kind) && executorDict.TryGetValue(kind, out executor)) {
                    TimeSpan time;
                    executor.Execute(message, out time);
                }
                else {
                }
            }
        }

        private string GetGroup(IMessage message)
        {
            if(message is EventStream)
                return "EventStream";

            if(message is IEvent)
                return "Event";

            if(message is ICommand)
                return "Command";

            return string.Empty;
        }

        //public void Start()
        //{
        //    lock(this.lockObject) {
        //        if(this.cancellationSource == null) {
        //            this.cancellationSource = new CancellationTokenSource();

        //            foreach(var broker in _brokers) {
        //                Task.Factory.StartNew(Consume, broker,
        //                    this.cancellationSource.Token,
        //                    TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
        //                    TaskScheduler.Current);
        //            }
        //        }
        //    }
        //}

        //public void Stop()
        //{
        //    lock(this.lockObject) {
        //        if(this.cancellationSource != null) {
        //            using(this.cancellationSource) {
        //                this.cancellationSource.Cancel();
        //                this.cancellationSource = null;
        //            }
        //        }
        //    }
        //}
        

        public void Initialize(IEnumerable<Type> types)
        {
            ((DefaultHandlerProvider)_handlerProvider).Initialize(types);


            if(this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();

                foreach(var broker in _brokers) {
                    Task.Factory.StartNew(Consume, broker,
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                        TaskScheduler.Current);
                }
            }
        }

        #endregion
    }
}
