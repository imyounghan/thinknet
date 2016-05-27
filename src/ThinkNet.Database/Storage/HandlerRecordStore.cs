using System;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Configurations;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 将已完成的处理消息的程序信息记录在数据库中。
    /// </summary>
    [RegisterComponent(typeof(IHandlerRecordStore))]
    public class HandlerRecordStore : HandlerRecordInMemory
    {
        private readonly IDataContextFactory _contextFactory;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public HandlerRecordStore(IDataContextFactory contextFactory)
        {
            this._contextFactory = contextFactory;
        }

        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        public override void AddHandlerInfo(string messageId, Type messageType, Type handlerType)
        {
            var messageTypeName = messageType.FullName;
            var handlerTypeName = handlerType.FullName;

            base.AddHandlerInfo(messageId, messageTypeName, handlerTypeName);

            var handlerRecord = new HandlerRecord(messageId, messageTypeName, handlerTypeName);
            Task.Factory.StartNew(() => {
                using (var context = _contextFactory.CreateDataContext()) {
                    var executed = context.CreateQuery<HandlerRecord>()
                        .Any(p => p.MessageId == handlerRecord.MessageId &&
                            p.MessageTypeCode == handlerRecord.MessageTypeCode &&
                            p.HandlerTypeCode == handlerRecord.HandlerTypeCode);
                    if (executed)
                        return;

                    context.Save(handlerRecord);
                    context.Commit();
                }
            });
        }

        protected override void Initialize()
        {
            Task.Factory.StartNew(() => {
                using (var context = _contextFactory.CreateDataContext()) {
                    var query = context.CreateQuery<HandlerRecord>();

                    return query.Take(1000).OrderByDescending(p => p.Timestamp).ToList();
                }
            }).Result.ForEach(record => {
                base.AddHandlerInfo(record.MessageId, record.MessageType, record.HandlerType);
            });
        }
    }
}
