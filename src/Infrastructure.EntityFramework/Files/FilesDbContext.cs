using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.EntityFramework.Files
{ 
    public partial class FilesDbContext : DbContext
    {
        public FilesDbContext(DbContextOptions<FilesDbContext> options, bool autoDetectChanges) : base(options)
        {
            this.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }

        public DbSet<FileEntity> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .ApplyConfiguration(new FileEntityConfig());
        }
    }
}
