

namespace ThinkNet.Messaging
{
    using System.Collections.Generic;

    /// <summary>
    /// 表示事件总线的接口。
    /// </summary>
    public interface IEventBus
    {
        void Publish(IEnumerable<IEvent> @events, int version, Envelope<Command> command);
    }
}
