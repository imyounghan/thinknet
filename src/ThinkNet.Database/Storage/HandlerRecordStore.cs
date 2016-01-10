using System.Linq;
using ThinkNet.Common;
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
        protected override void AddHandlerInfo(string messageId, int messageTypeCode, int handlerTypeCode)
        {
            using (var context = _contextFactory.CreateDataContext()) {
                if (IsHandlerInfoExist(context, messageId, messageTypeCode, handlerTypeCode))
                    return;

                var handlerInfo = new HandlerRecord(messageId, messageTypeCode, handlerTypeCode);
                context.Save(handlerInfo);
                context.Commit();
            }
        }

        private static bool IsHandlerInfoExist(IDataContext context, string messageId, int messageTypeCode, int handlerTypeCode)
        {
            return context.CreateQuery<HandlerRecord>()
                .Any(p => p.MessageId == messageId &&
                    p.MessageTypeCode == messageTypeCode &&
                    p.HandlerTypeCode == handlerTypeCode);
        }

        /// <summary>
        /// 检查该处理程序信息是否存在
        /// </summary>
        protected override bool IsHandlerInfoExist(string messageId, int messageTypeCode, int handlerTypeCode)
        {
            using (var context = _contextFactory.CreateDataContext()) {
                return IsHandlerInfoExist(context, messageId, messageTypeCode, handlerTypeCode);
            }
        } 
    }
}
