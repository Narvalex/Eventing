using System.Data.Entity;

namespace Eventing.OfflineClient.EntityFramework
{
    public class PendingMessageQueueDbContext : DbContext
    {
        public PendingMessageQueueDbContext(bool ifForReadOnly, string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            if (ifForReadOnly)
                this.Configuration.AutoDetectChangesEnabled = false;
        }

        public IDbSet<PendingMessageEntity> PendingMessageQueue { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new PendingMessageEntityMap());
        }
    }
}
