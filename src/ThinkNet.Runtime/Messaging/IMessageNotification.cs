using System;
using System.Collections.Generic;
using ThinkLib.Common;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示消息的通知接口
    /// </summary>
    [UnderlyingComponent(typeof(DefaultMessageNotification))]
    public interface IMessageNotification
    {
        /// <summary>
        /// 通知消息已完成
        /// </summary>
        void NotifyMessageCompleted(string messageId, Exception exception = null);

        /// <summary>
        /// 通知消息已处理
        /// </summary>
        void NotifyMessageHandled(string messageId, Exception exception = null);

        /// <summary>
        /// 通知消息未有任何动作
        /// </summary>
        void NotifyMessageUntreated(string messageId);
    }


    internal class DefaultMessageNotification : CommandResultManager, IMessageNotification, IInitializer
    {
        //public readonly static DefaultMessageNotification Instance = new DefaultMessageNotification();
        public DefaultMessageNotification(ICommandBus commandBus)
            : base(commandBus)
        { }

        #region IMessageNotification 成员

        public void NotifyMessageCompleted(string messageId, Exception exception = null)
        {
            this.NotifyCommandCompleted(messageId,
                exception.IsNull() ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyMessageHandled(string messageId, Exception exception = null)
        {
            this.NotifyCommandExecuted(messageId,
                exception.IsNull() ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyMessageUntreated(string messageId)
        {
            this.NotifyCommandCompleted(messageId, CommandStatus.NothingChanged, null);
        }

        #endregion

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            Bootstrapper.Current.RegisterInstance(typeof(ICommandResultManager), this);
        }

        #endregion
    }
}
