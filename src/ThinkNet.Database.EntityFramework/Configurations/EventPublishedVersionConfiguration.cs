using System.Data.Entity.ModelConfiguration;
using ThinkNet.Infrastructure.Storage;

namespace ThinkNet.Database.Configurations
{
    public class EventPublishedVersionConfiguration : EntityTypeConfiguration<EventPublishedVersion>
    {
        public EventPublishedVersionConfiguration()
        {
            this.HasKey(model => new { model.AggregateRootId, model.AggregateRootTypeCode });

            this.ToTable("thinknet_versions");

            this.Property(model => model.AggregateRootId).IsRequired().HasColumnType("char").HasMaxLength(32);
            this.Property(model => model.AggregateRootTypeCode);
            this.Property(model => model.Version);
            this.Property(model => model.AggregateRootTypeName).HasColumnType("varchar");
        }
    }
}
