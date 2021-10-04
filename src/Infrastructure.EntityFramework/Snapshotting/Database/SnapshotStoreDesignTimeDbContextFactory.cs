using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.EntityFramework.Snapshotting.Database
{
    public class SnapshotStoreDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SnapshotStoreDbContext>
    {
        public SnapshotStoreDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SnapshotStoreDbContext>();
            builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=SnapshotStoreDbContext;Trusted_Connection=True;MultipleActiveResultSets=true");
            return new SnapshotStoreDbContext(builder.Options, false);
        }
    }
}
