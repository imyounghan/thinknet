using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;


namespace ThinkNet.Infrastructure
{
    public class ThinkNetDbContext : DbContext
    {
        public ThinkNetDbContext()
        {
            this.Configuration.AutoDetectChangesEnabled = true;
            this.Configuration.LazyLoadingEnabled = true;
        }
        public ThinkNetDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            this.Configuration.AutoDetectChangesEnabled = true;
            this.Configuration.LazyLoadingEnabled = true;
        }
        public ThinkNetDbContext(DbConnection dbConnection)
            : base(dbConnection, false)
        {
            this.Configuration.AutoDetectChangesEnabled = true;
            this.Configuration.LazyLoadingEnabled = true;
        }


        private EntityTypeConfiguration<Event> EventDataConfiguration()
        {
            var config = new EntityTypeConfiguration<Event>();

            config.HasKey(@event => new { @event.AggregateRootId, @event.AggregateRootTypeCode, @event.Version, @event.Order });
            config.Property(@event => @event.AggregateRootId).IsRequired().HasColumnType("char").HasMaxLength(36);
            config.Property(@event => @event.AggregateRootTypeCode).IsRequired().HasColumnType("int");
            config.Property(@event => @event.AggregateRootTypeName).IsRequired().HasColumnType("varchar");
            config.Property(@event => @event.Order).HasColumnType("int");
            config.Property(@event => @event.Version).HasColumnType("int");
            config.Property(@event => @event.CorrelationId).HasColumnType("char").HasMaxLength(36);
            config.Property(@event => @event.AssemblyName).HasColumnType("varchar");
            config.Property(@event => @event.AssemblyName).HasColumnType("varchar");
            config.Property(@event => @event.Payload).HasColumnType("varchar");
            config.Property(@event => @event.Timestamp).HasColumnName("OnCreated").HasColumnType("datetime");

            config.ToTable("thinknet_events");

            return config;
        }

        private EntityTypeConfiguration<HandlerRecord> HandlerInfoConfiguration()
        {
            var config = new EntityTypeConfiguration<HandlerRecord>();

            config.HasKey(handler => new { handler.MessageId, handler.MessageTypeCode, handler.HandlerTypeCode });
            config.Property(handler => handler.MessageId).IsRequired().HasColumnType("char").HasMaxLength(36);
            config.Property(handler => handler.MessageTypeCode).IsRequired().HasColumnType("int");
            config.Property(handler => handler.HandlerTypeCode).HasColumnType("int");
            config.Property(handler => handler.Timestamp).HasColumnName("OnCreated").HasColumnType("datetime");

            config.ToTable("thinknet_handlers");

            return config;
        }

        private EntityTypeConfiguration<Snapshot> SnapshotConfiguration()
        {
            var config = new EntityTypeConfiguration<Snapshot>();

            config.HasKey(snapshot => new { snapshot.AggregateRootId, snapshot.AggregateRootTypeCode });
            config.Property(snapshot => snapshot.AggregateRootId).IsRequired().HasColumnType("char").HasMaxLength(36);
            config.Property(snapshot => snapshot.AggregateRootTypeCode).IsRequired().HasColumnType("int");
            config.Property(snapshot => snapshot.Version).HasColumnType("int");
            config.Property(snapshot => snapshot.Data).HasColumnType("varchar");
            config.Property(snapshot => snapshot.Timestamp).HasColumnName("OnCreated").HasColumnType("datetime");

            config.ToTable("thinknet_snapshots");

            return config;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations
                .Add(EventDataConfiguration())
                .Add(HandlerInfoConfiguration())
                .Add(SnapshotConfiguration());
        }
    }
}
