using ThinkLib.Contexts;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示创建事件上下文的工厂接口
    /// </summary>
    public interface IEventContextFactory
    {
        IEventContext CreateEventContext();

        IEventContext GetEventContext();
    }
}
