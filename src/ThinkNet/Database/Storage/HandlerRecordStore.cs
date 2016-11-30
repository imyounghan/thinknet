using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 将已完成的处理消息的程序信息记录在数据库中。
    /// </summary>
    public sealed class HandlerRecordStore : MessageHandlerRecordInMemory
    {
        private readonly IDataContextFactory _dataContextFactory;
        private readonly BlockingCollection<HandlerRecord> _queue;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public HandlerRecordStore(IDataContextFactory dataContextFactory)
        {
            this._dataContextFactory = dataContextFactory;
            this._queue = new BlockingCollection<HandlerRecord>();

            Task.Factory.StartNew(() => {
                using (var context = _dataContextFactory.Create()) {
                    var query = context.CreateQuery<HandlerRecord>();

                    return query.Take(1000).OrderByDescending(p => p.Timestamp).ToList();
                }
            }).ContinueWith(task => {
                if (task.Status == TaskStatus.Faulted)
                    return;

                foreach (var record in task.Result) {
                    base.AddHandlerInfo(record.MessageId, record.MessageTypeName, record.HandlerTypeName);
                }
            });
        }

        private void BatchSaveHandlerInfo()
        {
            Task.Factory.StartNew(() => {
                int count = 0;
                using(var context = _dataContextFactory.Create()) {
                    HandlerRecord handlerRecord;

                    while(count++ < 20 && _queue.TryTake(out handlerRecord)) {
                        //var executed = context.CreateQuery<HandlerRecord>()
                        //.Any(p => p.MessageId == handlerRecord.MessageId &&
                        //    p.MessageTypeCode == handlerRecord.MessageTypeCode &&
                        //    p.HandlerTypeCode == handlerRecord.HandlerTypeCode);
                        //if(!executed)
                        context.Save(handlerRecord);
                    }
                    context.Commit();
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
            _queue.Add(handlerRecord);
        }

    }
}
