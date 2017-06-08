
namespace ThinkNet.Messaging
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    using ThinkNet.Infrastructure;

    public class MessageProducer<TMessage> : MessageReceiver<TMessage>, IMessageBus<TMessage>
        where TMessage : IMessage
    {
        public MessageProducer(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.GetDefault();
            this.broker = new BlockingCollection<Envelope<TMessage>>();
        }

        /// <summary>
        /// 写日志程序
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// 消息队列
        /// </summary>
        private readonly BlockingCollection<Envelope<TMessage>> broker;

        /// <summary>
        /// 将该消息追加到队列末尾，如果成功则返回true，失败则返回false（出现这种情况是由于队列已满）
        /// </summary>
        protected bool Append(Envelope<TMessage> message)
        {
            if (this.broker.TryAdd(message))
            {
                if (this.logger.IsDebugEnabled)
                {
                    this.logger.DebugFormat("Add a message({0}) to local queue.", message.Body);
                }
                return true;
            }
            else
            {
                if(this.logger.IsDebugEnabled) {
                    this.logger.DebugFormat("Add a message({0}) to local queue failed.", message.Body);
                }
                return false;
            }
        }

        /// <summary>
        /// 从队列里取出消息
        /// </summary>
        /// <param name="cancellationToken">通知取消的令牌</param>
        protected override void ReceiveMessages(CancellationToken cancellationToken)
        {
            foreach(var item in broker.GetConsumingEnumerable(cancellationToken)) {
                if(this.logger.IsDebugEnabled) {
                    this.logger.DebugFormat(
                        "Take a message({0}) from local queue, data:({1}).",
                        item.Body.GetType().FullName,
                        item.Body);
                }

                this.OnMessageReceived(this, item);
            }
        }

        #region IMessageBus<TMessage> 成员

        public virtual void Send(TMessage message)
        {
            this.Send(new Envelope<TMessage>(message) { MessageId = ObjectId.GenerateNewStringId() });
        }

        public void Send(Envelope<TMessage> message)
        {
            this.Append(message);
        }

        public virtual void Send(IEnumerable<TMessage> messages)
        {
            if(messages == null) {
                return;
            }

            messages.ForEach(this.Send);
        }

        public void Send(IEnumerable<Envelope<TMessage>> messages)
        {
            if(messages == null) {
                return;
            }

            messages.ForEach(this.Send);
        }

        #endregion
    }
}
