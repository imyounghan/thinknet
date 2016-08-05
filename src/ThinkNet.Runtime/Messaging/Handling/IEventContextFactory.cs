
namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示创建事件上下文的工厂接口
    /// </summary>
    public interface IEventContextFactory
    {
        /// <summary>
        /// 绑定当前上下文
        /// </summary>
        void Bind();
        /// <summary>
        /// 解绑当前上下文
        /// </summary>
        /// <param name="allHandlerCompleted">表示所有的EventHandler已完成</param>
        void Unbind(bool allHandlerCompleted);

        /// <summary>
        /// 获取当前上下文
        /// </summary>
        IEventContext GetEventContext();
    }
}
