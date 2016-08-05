
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个消息。
    /// </summary>
    public interface IMessage
    { 
        /// <summary>
        /// 获取消息ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 创建时间(GMT+0)
        /// </summary>
        System.DateTime CreatedTime { get; }
    }
}
