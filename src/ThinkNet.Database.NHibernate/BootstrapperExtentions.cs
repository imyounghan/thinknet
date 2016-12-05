using System.Configuration;
using NhCfg = NHibernate.Cfg;
using ThinkNet.Database;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Infrastructure.Storage;
using ThinkNet.Messaging.Handling;

namespace ThinkNet
{
    public static class BootstrapperExtentions
    {
        public static Bootstrapper UseNHibernate(this Bootstrapper that, string contextType = null)
        {
            that.SetDefault<IEventStore, EventStore>();
            that.SetDefault<IEventPublishedVersionStore, EventPublishedVersionStore>();
            that.SetDefault<ISnapshotStore, SnapshotStore>();
            that.SetDefault<IMessageHandlerRecordStore, MessageHandlerRecordStore>();

            NHibernateSessionBuilder.BuildSessionFactory(new NhCfg.Configuration());
            contextType = contextType.IfEmpty(() => ConfigurationManager.AppSettings["thinkcfg_dbcontextmode"]).IfEmpty("thread");
            return that.SetDefault<IDataContextFactory>(new NHibernateContextFactory(contextType));
        }
    }
}
