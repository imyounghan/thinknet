namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示命令处理结果的通知接口
    /// </summary>    
    public interface ICommandResultNotification
    {
        /// <summary>
        /// 通知命令结果
        /// </summary>
        void Notify(string commandId, ICommandResult commandResult, CommandReturnMode returnMode);
    }
}
