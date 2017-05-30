
namespace ThinkNet.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class MessageReceiver<TMessage> : IMessageReceiver<Envelope<TMessage>>
    {
        /// <summary>
        /// 通知源
        /// </summary>
        private CancellationTokenSource cancellationSource;

        #region IMessageReceiver<Envelope<TMessage>> 成员

        public event EventHandler<Envelope<TMessage>> MessageReceived = (sender, args) => { };

        public void Start()
        {
            if(this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();

                Task.Factory.StartNew(this.ReceiveMessages,
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                        TaskScheduler.Current);
            }
        }

        public void Stop()
        {
            if(this.cancellationSource != null) {
                using(this.cancellationSource) {
                    this.cancellationSource.Cancel();
                    this.cancellationSource = null;
                }
            }
        }

        protected virtual void OnMessageReceived(object sender, Envelope<TMessage> message)
        {
            this.MessageReceived(sender, message);
        }

        private void ReceiveMessages()
        {
            this.ReceiveMessages(this.cancellationSource.Token);
        }

        protected abstract void ReceiveMessages(CancellationToken cancellationToken);

        #endregion
    }
}
