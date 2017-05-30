
namespace ThinkNet.Messaging
{
    using System.Collections.Concurrent;
    using System.Threading;

    using ThinkNet.Infrastructure;

    /// <summary>
    /// 消息主机
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    public class MessageBroker<TMessage> : MessageReceiver<TMessage>
    {
        /// <summary>
        /// 写日志程序
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// 消息队列
        /// </summary>
        private readonly BlockingCollection<Envelope<TMessage>> broker;
        
        public MessageBroker(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.GetDefault();
            this.broker = new BlockingCollection<Envelope<TMessage>>();
        }

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
    }
}
