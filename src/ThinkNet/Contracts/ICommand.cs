
namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示这是一个命令
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 命令标识
        /// </summary>
        string Id { get; }
    }
}
