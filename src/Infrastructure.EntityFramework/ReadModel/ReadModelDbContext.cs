#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{
    public abstract class ReadModelDbContext : DbContext
    {
        private const string INVALID_SAVECHANGES_CALL = "Invalid save changes called in ReadModelDbContext";

        protected ReadModelDbContext([NotNull] DbContextOptions options, bool autoDetectChanges) : base(options)
        {
            this.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }

        public DbSet<ReadModelCheckpointEntity> Checkpoints { get; set; }

        public static string ResolveReadModelName<T>() where T : ReadModelDbContext =>
            ResolveReadModelName(typeof(T));

        public static string ResolveReadModelName(Type dbContextType) => 
            dbContextType.Name.Split("DbContext").First();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ReadModelCheckpointEntityConfig());
        }

        internal Task<int> SafeSaveChangesAsync()
            => base.SaveChangesAsync();

        /// <summary>
        /// I sure hope you know what your're doing...
        /// </summary>
        public Task<int> UnsafeSaveChangesAsync()
            => base.SaveChangesAsync();

        /// <summary>
        /// This method is hidden. Do not call this on projection time. The underlying projection engine will call this for you 
        /// at any time, without determinism.
        /// </summary>
        public new int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new InvalidOperationException(INVALID_SAVECHANGES_CALL);
        }

        /// <summary>
        /// This method is hidden. Do not call this on projection time. The underlying projection engine will call this for you 
        /// at any time, without determinism.
        /// </summary>
        public new int SaveChanges()
        {
            throw new InvalidOperationException(INVALID_SAVECHANGES_CALL);
        }

        /// <summary>
        /// This method is hidden. Do not call this on projection time. The underlying projection engine will call this for you 
        /// at any time, without determinism.
        /// </summary>
        public new Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(INVALID_SAVECHANGES_CALL);
        }

        /// <summary>
        /// This method is hidden. Do not call this on projection time. The underlying projection engine will call this for you 
        /// at any time, without determinism.
        /// </summary>
        public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(INVALID_SAVECHANGES_CALL);
        }
    }
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
