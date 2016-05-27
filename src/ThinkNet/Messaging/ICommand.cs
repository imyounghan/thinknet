using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个命令。
    /// </summary>
    public interface ICommand : IMessage
    {
        ///// <summary>
        ///// 获取命令ID
        ///// </summary>
        //string Id { get; }
    }
}
