using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.EntityFramework.ReadModel
{
    public class ReadModelCheckpointEntity
    {
        public const string IDEMPOTENCY_CHK = "idempotency";
        public const string SUBSCRIPTION_CHK = "subscription";

        public string Id { get; set; } = null!;
        public long EventNumber { get; set; }
        public long CommitPosition { get; set; }
        public long PreparePosition { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class ReadModelCheckpointEntityConfig : IEntityTypeConfiguration<ReadModelCheckpointEntity>
    {
        public void Configure(EntityTypeBuilder<ReadModelCheckpointEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.ToTable("Checkpoints");
        }
    }
}
