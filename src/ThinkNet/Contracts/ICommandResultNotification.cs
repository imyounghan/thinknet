using System;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示命令处理结果的通知接口
    /// </summary>
    public interface ICommandResultNotification
    {
        /// <summary>
        /// 通知命令已完成
        /// </summary>
        void NotifyCommandHandled(CommandResult commandResult);

        /// <summary>
        /// 通知由命令产生的事件已处理
        /// </summary>
        void NotifyEventHandled(CommandResult commandResult);

        ///// <summary>
        ///// 通知命令未有修改聚合的操作
        ///// </summary>
        //void NotifyUnchanged(string commandId);

        //void Notify(CommandResult commandResult);
    }
}
