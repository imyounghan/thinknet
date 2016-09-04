using System.Data.Entity.ModelConfiguration;

namespace ThinkNet.Infrastructure
{
    public class EventDataItemConfiguration : EntityTypeConfiguration<EventDataItem>
    {
        public EventDataItemConfiguration()
        {
            this.HasKey(model => new { model.EventId, model.Order });

            this.ToTable("thinknet_eventitems");

            this.Property(model => model.EventId);
            this.Property(model => model.Order);
            this.Property(model => model.AssemblyName).IsRequired().HasColumnType("varchar");
            this.Property(model => model.Namespace).IsRequired().HasColumnType("varchar");
            this.Property(model => model.TypeName).IsRequired().HasColumnType("varchar");
            this.Property(model => model.Payload);
        }
    }
}
