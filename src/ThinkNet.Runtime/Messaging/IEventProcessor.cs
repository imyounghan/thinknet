using ThinkNet.Annotation;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Runtime
{
    [RequiredComponent(typeof(DefaultEventProcessor))]
    public interface IEventProcessor : IMessageProcessor<IEvent>
    { }
}
