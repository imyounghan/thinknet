using System.Configuration;
using ThinkNet.Database;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Infrastructure.Storage;
using ThinkNet.Messaging.Handling;

namespace ThinkNet
{
    public static class BootstrapperExtentions
    {
        public static Bootstrapper UseEntityFramework(this Bootstrapper that)
        {
            return that.UseEntityFramework(new DefaultContextFactory());
        }

        public static Bootstrapper UseEntityFramework(this Bootstrapper that, IDbContextFactory contextFactory, string contextType = null)
        {
            that.SetDefault<IEventStore, EventStore>();
            that.SetDefault<IEventPublishedVersionStore, EventPublishedVersionStore>();
            that.SetDefault<ISnapshotStore, SnapshotStore>();
            that.SetDefault<IMessageHandlerRecordStore, MessageHandlerRecordStore>();

            contextType = contextType.IfEmpty(() => ConfigurationManager.AppSettings["thinkcfg_dbcontextmode"]).IfEmpty("thread");
            return that.SetDefault<IDataContextFactory>(new EntityFrameworkContextFactory(contextFactory, contextType));
        }
    }
}
