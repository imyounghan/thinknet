using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class MessageBus : DisposableObject, ICommandBus, IEventBus//, IInitializer
    {
        //private readonly BlockingCollection<IMessage> _broker;
        private readonly IEnvelopeSender _sender;
        private readonly IRoutingKeyProvider _routingKeyProvider;
        //private readonly ISerializer _serializer;

        //private CancellationTokenSource cancellationSource;

        public MessageBus(IEnvelopeSender sender, 
            IRoutingKeyProvider routingKeyProvider/*, 
            ISerializer serializer*/)
        {
            this._sender = sender;
            this._routingKeyProvider = routingKeyProvider;
            //this._serializer = serializer;

            //this._broker = new BlockingCollection<IMessage>();
        }

        #region IEventBus 成员
        public virtual void Publish(IEnumerable<IEvent> events)
        {
            //foreach(var @event in events) {
            //    _broker.Add(@event);
            //}
            _sender.SendAsync(events.Select(Transform));
        }

        public void Publish(IEvent @event)
        {
            _sender.SendAsync(Transform(@event));
        }
        
        #endregion

        #region ICommandBus 成员
        public virtual void Send(IEnumerable<ICommand> commands)
        {
            //foreach(var command in commands) {
            //    _broker.Add(command);
            //}
            _sender.SendAsync(commands.Select(Transform));
        }

        public void Send(ICommand command)
        {
            _sender.SendAsync(Transform(command)).Wait();
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            //if (disposing && this.cancellationSource != null) {
            //    using (this.cancellationSource) {
            //        this.cancellationSource.Cancel();
            //        this.cancellationSource = null;
            //    }
            //}
        }

        private Envelope Transform(IMessage message)
        {
            var routingKey = _routingKeyProvider.GetRoutingKey(message);
            //var playload = _serializer.Serialize(message);
            //var type = message.GetType();

            return new Envelope() {
                Body = message,
                CorrelationId = message.Id,
                RoutingKey = routingKey,
            };
        }

        //private void Consume(object state)
        //{
        //    var broker = state as BlockingCollection<IMessage>;
        //    broker.NotNull("broker");

        //    //while(!cancellationSource.Token.IsCancellationRequested) {
        //    var messages = broker.GetConsumingEnumerable();
        //    _sender.SendAsync(messages.Select(Transform)).Wait();
        //    //}
        //}

        //public virtual void Initialize(IEnumerable<Type> types)
        //{
        //    foreach(var type in types.Where(TypeHelper.IsMessage)) {
        //        if(!type.IsSerializable) {
        //            string message = string.Format("{0} should be marked as serializable.", type.FullName);
        //            throw new ApplicationException(message);
        //        }
        //    }

        //    if (this.cancellationSource == null) {
        //        this.cancellationSource = new CancellationTokenSource();

        //        Task.Factory.StartNew(Consume, _broker,
        //                this.cancellationSource.Token,
        //                TaskCreationOptions.LongRunning,
        //                TaskScheduler.Current);
        //    }
        //}
    }
}
