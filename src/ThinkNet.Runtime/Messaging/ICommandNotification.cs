using System;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示命令的通知接口
    /// </summary>
    public interface ICommandNotification
    {
        /// <summary>
        /// 通知命令已完成
        /// </summary>
        void NotifyCompleted(string commandId, Exception exception = null);

        /// <summary>
        /// 通知命令已处理
        /// </summary>
        void NotifyHandled(string commandId, Exception exception = null);

        /// <summary>
        /// 通知命令未有修改聚合的操作
        /// </summary>
        void NotifyUnchanged(string commandId);
    }
}
