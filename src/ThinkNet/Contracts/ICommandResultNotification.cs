using System;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示命令处理结果的通知接口
    /// </summary>    
    public interface ICommandResultNotification
    {
        void Notify(string commandId, ICommandResult commandResult, CommandReturnType returnType);
    }
}
