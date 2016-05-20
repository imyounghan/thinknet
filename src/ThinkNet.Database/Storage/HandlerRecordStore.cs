using System;
using System.Linq;
using System.Threading.Tasks;
using ThinkLib.Common;
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
            base.AddHandlerInfo(messageId, messageType, handlerType);

            var handlerRecord = new HandlerRecord(messageId, messageType.FullName, handlerType.FullName);
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

        public override bool HandlerHasExecuted(string messageId, Type messageType, params Type[] handlerTypes)
        {
            var allExecuted = handlerTypes.All(handlerType => HandlerIsExecuted(messageId, messageType, handlerType));
            if (!allExecuted) {
                Task.Factory.StartNew(() => {
                    using (var context = _contextFactory.CreateDataContext()) {
                        var query =context.CreateQuery<HandlerRecord>();

                        foreach (var handlerType in handlerTypes) {
                            var executed = query.Any(p => p.MessageId == messageId &&
                                    p.MessageTypeCode == messageType.FullName.GetHashCode() &&
                                    p.HandlerTypeCode == handlerType.FullName.GetHashCode());

                            if (executed)
                                base.AddHandlerInfo(messageId, messageType, handlerType);
                        }
                    }
                }).Wait();
            }


            return base.HandlerHasExecuted(messageId, messageType, handlerTypes);
        }

        //private static bool IsHandlerInfoExist(IDataContext context, HandlerRecord handlerRecord)
        //{
        //    if (handlerRecord == null)
        //        return false;

        //    return context.CreateQuery<HandlerRecord>()
        //        .Any(p => p.MessageId == handlerRecord.MessageId &&
        //            p.MessageTypeCode == handlerRecord.MessageTypeCode &&
        //            p.HandlerTypeCode == handlerRecord.HandlerTypeCode);
        //}

        ///// <summary>
        ///// 检查该处理程序信息是否存在
        ///// </summary>
        //protected override bool CheckHandlerInfoExist(string messageId, Type messageType, Type handlerType)
        //{
        //    var handlerRecord = new HandlerRecord(messageId, messageType.FullName, handlerType.FullName);
        //    return Task.Factory.StartNew(() => {
        //        using (var context = _contextFactory.CreateDataContext()) {
        //            return IsHandlerInfoExist(context, handlerRecord);
        //        }
        //    }).Result;
        //} 
    }
}
