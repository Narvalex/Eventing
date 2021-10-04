using Infrastructure.EntityFramework.ReadModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.EntityFramework.EventLog
{
    public class EventLogDbContext : ReadModelDbContext
    {
        public EventLogDbContext([NotNull] DbContextOptions options, bool autoDetectChanges) : base(options, autoDetectChanges)
        {
        }

        public DbSet<EventEntity> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new EventEntityConfig());
        }
    }
}
