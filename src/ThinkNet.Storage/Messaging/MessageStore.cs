using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using ThinkNet.Components;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Base;
using ThinkNet.Serialization;


namespace ThinkNet.Messaging
{
    /// <summary>
    /// 消息存储器
    /// </summary>
    [RegisteredComponent(typeof(IMessageStore))]
    public class MessageStore : IMessageStore
    {
        private readonly ITextSerializer _serializer;
        private readonly IDataContextFactory _dbContextFactory;
        private readonly bool _persistent;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageStore(IDataContextFactory dbContextFactory, ITextSerializer serializer)
        {
            this._dbContextFactory = dbContextFactory;
            this._serializer = serializer;
            this._persistent = ConfigurationManager.AppSettings["thinkcfg.message_persist"].Safe("false").ToBoolean();
        }

        /// <summary>
        /// 是否启用持久化
        /// </summary>
        public bool PersistEnabled
        {
            get { return _persistent; }
        }

        /// <summary>
        /// 添加一组消息
        /// </summary>
        public void Add(string messageType, IEnumerable<IMessage> messages)
        {
            if (!_persistent || messages.IsEmpty())
                return;

            using (var context = _dbContextFactory.CreateDataContext()) {
                messages.Select(p => new MessageData {
                    MessageId = p.Id,
                    MessageType = messageType,
                    Body = _serializer.Serialize(p),
                    DeliveryDate = p.Timestamp
                }).ForEach(context.Save);

                context.Commit();
            }
        }

        /// <summary>
        /// 移除消息
        /// </summary>
        public void Remove(string messageType, string messageId)
        {
            if (!_persistent)
                return;

            using (var context = _dbContextFactory.CreateDataContext()) {           
                context.Delete(new MessageData {
                    MessageType = messageType,
                    MessageId = messageId
                });
                context.Commit();
            }
        }

        /// <summary>
        /// 获取该消息类型的所有消息
        /// </summary>
        public IEnumerable<IMessage> GetAll(string messageType)
        {
            if (!_persistent)
                return Enumerable.Empty<IMessage>();

            using (var context = _dbContextFactory.CreateDataContext()) {
                return context.CreateQuery<MessageData>()
                    .Where(message => message.MessageType == messageType)
                    .OrderBy(@event => @event.DeliveryDate)
                    .AsEnumerable()
                    .Select(p => _serializer.Deserialize(p.Body))
                    .OfType<IMessage>();
            }
        }
    }
}
