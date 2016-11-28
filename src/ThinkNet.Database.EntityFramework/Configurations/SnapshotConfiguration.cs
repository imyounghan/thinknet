using System.Data.Entity.ModelConfiguration;
using ThinkNet.Database.Storage;

namespace ThinkNet.Infrastructure
{
    public class SnapshotConfiguration : EntityTypeConfiguration<Snapshot>
    {
        public SnapshotConfiguration()
        {
            this.HasKey(model => new { model.AggregateRootId, model.AggregateRootTypeCode, model.Version });

            this.ToTable("thinknet_snapshots");

            this.Property(model => model.AggregateRootId).IsRequired().HasColumnType("char").HasMaxLength(32);
            this.Property(model => model.AggregateRootTypeCode);
            this.Property(model => model.Version);
            this.Property(model => model.AggregateRootTypeName).HasColumnType("varchar");            
            this.Property(model => model.Data);
            this.Property(model => model.Timestamp).HasColumnName("OnCreated");

            
        }
    }
}
