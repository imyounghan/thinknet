using System;
using System.Collections.Generic;
using ThinkLib.Common;
using ThinkNet.Kernel;


namespace ThinkNet.Messaging.Handling
{
    //[RegisterComponent(typeof(IProcessor), "MessageProcessor")]
    public class MessageProcessor : Processor, IInitializer
    {
        private readonly IMessageExecutor _executor;
        private readonly IMessageNotification _notification;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageProcessor(IMessageReceiver receiver, IMessageExecutor executor, IMessageNotification notification)
            : base(receiver)
        {
            this._executor = executor;
            this._notification = notification;
        }

        protected override void Process(IMessage message)
        {
            Exception exception = null;
            try {
                _executor.Execute(message);
            }
            catch (HandlerRecordStoreException) {
                throw;
            }
            catch (Exception ex) {
                exception = ex;
                throw ex;
            }
            finally {
                Notify(message, exception);
            }
        }

        private void Notify(IMessage message, Exception exception)
        {
            //var command = message as ICommand;
            if (message is ICommand) {
                _notification.NotifyMessageHandled(message.Id, exception);
                return;
            }

            //var @event = message as EventStream;
            //if (@event != null) {
            //    if (@event.Events.IsEmpty()) {
            //        _notification.NotifyMessageUntreated(@event.CommandId);
            //        return;
            //    }
            //    _notification.NotifyMessageCompleted(@event.CommandId, exception);
            //    return;
            //}
        }
                

        #region IInitializer 成员
        public void Initialize(IEnumerable<Type> types)
        {
            this.Start();
        }

        #endregion
    }
}
