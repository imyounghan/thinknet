using ThinkNet.Annotation;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Runtime
{
    [RequiredComponent(typeof(DefaultCommandProcessor))]
    public interface ICommandProcessor : IMessageProcessor<ICommand>
    { }
}
