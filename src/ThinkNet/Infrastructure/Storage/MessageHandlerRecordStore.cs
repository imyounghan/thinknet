using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Database;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Infrastructure.Storage
{
    /// <summary>
    /// 将已完成的处理消息的程序信息记录在数据库中。
    /// </summary>
    public sealed class MessageHandlerRecordStore : MessageHandlerRecordInMemory
    {
        private readonly IDataContextFactory _dataContextFactory;
        private readonly ConcurrentQueue<MessageHandlerRecord> _queue;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public MessageHandlerRecordStore(IDataContextFactory dataContextFactory)
        {
            this._dataContextFactory = dataContextFactory;
            this._queue = new ConcurrentQueue<MessageHandlerRecord>();

            Task.Factory.StartNew(() => {
                using (var context = _dataContextFactory.Create()) {
                    var query = context.CreateQuery<MessageHandlerRecord>();

                    return query.Take(1000).OrderByDescending(p => p.Timestamp).ToList();
                }
            }).ContinueWith(task => {
                if (task.Status == TaskStatus.Faulted)
                    return;

                foreach (var record in task.Result) {
                    base.AddHandlerInfoToMemory(record.MessageId, record.MessageTypeName, record.HandlerTypeName);
                }
            });
        }

        /// <summary>
        /// 批量保存
        /// </summary>
        protected override void TimeProcessing()
        {
            if(_queue.Count == 0)
                return;

            int count = 0;
            try {
                using(var context = _dataContextFactory.Create()) {
                    MessageHandlerRecord handlerRecord;

                    while(count++ < 20 && _queue.TryDequeue(out handlerRecord)) {
                        //var executed = context.CreateQuery<HandlerRecord>()
                        //.Any(p => p.MessageId == handlerRecord.MessageId &&
                        //    p.MessageTypeCode == handlerRecord.MessageTypeCode &&
                        //    p.HandlerTypeCode == handlerRecord.HandlerTypeCode);
                        //if(!executed)
                        context.Save(handlerRecord);
                    }
                    context.Commit();
                }
            }
            catch(Exception ex) {
                if(LogManager.Default.IsErrorEnabled) {
                    LogManager.Default.Error("", ex);
                }
            }
            
        }

        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        public override void AddHandlerInfo(string messageId, Type messageType, Type handlerType)
        {
            var handlerRecord = new MessageHandlerRecord(messageId, messageType, handlerType);
            _queue.Enqueue(handlerRecord);
        }
        
    }
}
