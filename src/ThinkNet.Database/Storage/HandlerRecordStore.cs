using System;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Messaging.Handling;
using ThinkLib.Common;

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
            base.AddHandlerInfo(messageId, messageType, handlerType);

            Task.Factory.StartNew((state) => {
                using (var context = _contextFactory.CreateDataContext()) {
                    if (IsHandlerInfoExist(context, state as HandlerRecord))
                        return;

                    context.Save(state);
                    context.Commit();
                }
            }, new HandlerRecord(messageId, messageType.FullName, handlerType.FullName)).Wait();
            
        }

        private static bool IsHandlerInfoExist(IDataContext context, HandlerRecord handlerRecord)
        {
            if (handlerRecord == null)
                return false;

            return context.CreateQuery<HandlerRecord>()
                .Any(p => p.MessageId == handlerRecord.MessageId &&
                    p.MessageTypeCode == handlerRecord.MessageTypeCode &&
                    p.HandlerTypeCode == handlerRecord.HandlerTypeCode);
        }

        /// <summary>
        /// 检查该处理程序信息是否存在
        /// </summary>
        protected override bool CheckHandlerInfoExist(string messageId, Type messageType, Type handlerType)
        {
            var task = Task.Factory.StartNew((state) => {
                using (var context = _contextFactory.CreateDataContext()) {
                    return IsHandlerInfoExist(context, state as HandlerRecord);
                }
            }, new HandlerRecord(messageId, messageType.FullName, handlerType.FullName));
            task.Wait();

            return task.Result;
        } 
    }
}
