using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个消息。
    /// </summary>
    public interface IMessage// : IDataTransferObject
    {
        /// <summary>
        /// 获取当前消息的关键字符串
        /// </summary>
        string GetKey();
    }
}
