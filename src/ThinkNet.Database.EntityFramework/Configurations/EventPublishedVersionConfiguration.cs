using System.Data.Entity.ModelConfiguration;

namespace ThinkNet.Infrastructure
{
    public class EventPublishedVersionConfiguration : EntityTypeConfiguration<EventPublishedVersion>
    {
        public EventPublishedVersionConfiguration()
        {
            this.HasKey(model => new { model.AggregateRootId, model.AggregateRootTypeCode });

            this.ToTable("thinknet_eventversions");

            this.Property(model => model.AggregateRootId).IsRequired().HasColumnType("char").HasMaxLength(32);
            this.Property(model => model.AggregateRootTypeCode);
            this.Property(model => model.AggregateRootTypeName).HasColumnType("varchar");
            this.Property(model => model.Version);            
        }
    }
}
