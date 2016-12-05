using System.Data.Entity.ModelConfiguration;
using ThinkNet.Infrastructure.Storage;

namespace ThinkNet.Infrastructure.Configurations
{
    public class MessageHandlerRecordConfiguration : EntityTypeConfiguration<MessageHandlerRecord>
    {
        public MessageHandlerRecordConfiguration()
        {
            this.HasKey(model => new { model.MessageId, model.MessageTypeCode, model.HandlerTypeCode });

            this.ToTable("thinknet_handlers");

            this.Property(model => model.MessageId).IsRequired().HasColumnType("char").HasMaxLength(32);
            this.Property(model => model.MessageTypeCode);
            this.Property(model => model.HandlerTypeCode);
            this.Property(model => model.MessageTypeName).HasColumnType("varchar");
            this.Property(model => model.HandlerTypeName).HasColumnType("varchar");
            this.Property(model => model.Timestamp).HasColumnName("OnCreated");            
        }
    }
}
