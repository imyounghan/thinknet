
namespace ThinkNet.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class MessageReceiver<TMessage> : IMessageReceiver<Envelope<TMessage>>
        where TMessage : IMessage
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

        /// <summary>
        /// 当收到消息后的处理方法
        /// </summary>
        protected virtual void OnMessageReceived(object sender, Envelope<TMessage> message)
        {
            this.MessageReceived(sender, message);
        }

        private void ReceiveMessages()
        {
            this.ReceiveMessages(this.cancellationSource.Token);
        }

        /// <summary>
        /// 取出消息的方法
        /// </summary>
        /// <param name="cancellationToken">通知取消的令牌</param>
        protected abstract void ReceiveMessages(CancellationToken cancellationToken);

        #endregion
    }
}
