using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using ThinkNet.Infrastructure.Storage;

namespace ThinkNet.Infrastructure.Configurations
{
    public class EventDataConfiguration : EntityTypeConfiguration<EventData>
    {
        public EventDataConfiguration()
        {
            this.HasKey(model => new { model.AggregateRootId, model.AggregateRootTypeCode, model.Version });

            this.ToTable("thinknet_events");

            this.Property(model => model.EventId).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            this.Property(model => model.AggregateRootId).IsRequired().HasColumnType("char").HasMaxLength(32);
            this.Property(model => model.AggregateRootTypeCode);
            this.Property(model => model.Version);
            this.Property(model => model.AggregateRootTypeName).IsRequired().HasColumnType("varchar");            
            this.Property(model => model.CorrelationId).HasColumnType("char").HasMaxLength(32);
            this.Property(model => model.Timestamp).HasColumnName("OnCreated");
            this.HasMany(model => model.Items).WithRequired().HasForeignKey(item => item.EventId);
        }
    }
}
