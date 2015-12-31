
namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示继承该接口的类型是一个消息。
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// 获取消息标识
        /// </summary>
        string Id { get; }
        /// <summary>
        /// 获取生成该消息的时间戳
        /// </summary>
        System.DateTime OnCreated { get; }

        ///// <summary>
        ///// 获取路由key。
        ///// </summary>
        //string GetRoutingKey();
    }
}
