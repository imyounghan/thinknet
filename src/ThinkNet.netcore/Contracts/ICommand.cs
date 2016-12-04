using System.Runtime.Serialization;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示这是一个命令
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 标识ID
        /// </summary>
        [DataMember]
        string Id { get; }
    }
}
