using System;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Database;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 将已完成的处理消息的程序信息记录在数据库中。
    /// </summary>
    [Register(typeof(IHandlerRecordStore))]
    public class HandlerRecordStore : HandlerRecordInMemory
    {
        private readonly IDataContextFactory _contextFactory;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public HandlerRecordStore(IDataContextFactory contextFactory)
        {
            this._contextFactory = contextFactory;

            Task.Factory.StartNew(() => {
                using (var context = _contextFactory.CreateDataContext()) {
                    var query = context.CreateQuery<HandlerRecord>();

                    return query.Take(1000).OrderByDescending(p => p.Timestamp).ToList();
                }
            })
            .ContinueWith(task => {
                foreach (var record in task.Result) {
                    base.AddHandlerInfo(record.MessageId, record.MessageTypeName, record.HandlerTypeName);
                }
            });
        }

        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        public override void AddHandlerInfo(string messageId, Type messageType, Type handlerType)
        {
            base.AddHandlerInfo(messageId, messageType.FullName, handlerType.FullName);

            var handlerRecord = new HandlerRecord(messageId, messageType, handlerType);
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

    }
}
