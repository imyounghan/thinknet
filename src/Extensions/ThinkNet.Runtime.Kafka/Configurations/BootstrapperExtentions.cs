using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Runtime;

namespace ThinkNet.Configurations
{
    public static class BootstrapperExtentions
    {
        public static Bootstrapper UsingKafka(this Bootstrapper that)
        {
            that.RegisterType<ICommandBus, CommandBus>();
            that.RegisterType<IEventBus, EventBus>();
            that.RegisterType<ITopicProvider, DefaultTopicProvider>();
            that.RegisterType<IProcessor, KafkaProcessor>("KafkaProcessor");

            return that;
        }
    }
}
