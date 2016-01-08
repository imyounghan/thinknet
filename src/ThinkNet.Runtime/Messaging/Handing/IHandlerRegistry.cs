using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handing
{
    public interface IHandlerRegistry
    {
        void Register(IHandler handler);
    }
}
