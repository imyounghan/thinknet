
namespace ThinkNet.Common
{
    /// <summary>
    /// 表示消息的中转站
    /// </summary>
    public interface IEnvelopeHub
    {
        /// <summary>
        /// 消息转发
        /// </summary>
        void Distribute(object message);
    }
}
