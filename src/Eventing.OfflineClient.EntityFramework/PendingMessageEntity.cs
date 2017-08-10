using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Eventing.OfflineClient.EntityFramework
{
    public class PendingMessageEntity
    {
        public int Id { get; set; }
        public bool Sent { get; set; }
        public DateTime DateEnqueued { get; set; }
        public DateTime? DateSent { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public string Payload { get; set; }
    }

    public class PendingMessageEntityMap : EntityTypeConfiguration<PendingMessageEntity>
    {
        public PendingMessageEntityMap()
        {
            this.HasKey(e => e.Id);

            this.Property(e => e.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            this.ToTable("PendingMessageQueue");
        }
    }
}
