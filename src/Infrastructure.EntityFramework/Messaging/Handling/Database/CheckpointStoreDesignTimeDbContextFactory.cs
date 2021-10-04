using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.EntityFramework.Messaging.Handling.Database
{
    public class CheckpointStoreDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CheckpointStoreDbContext>
    {
        public CheckpointStoreDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<CheckpointStoreDbContext>();
            builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CheckpointDbContext;Trusted_Connection=True;MultipleActiveResultSets=true");
            return new CheckpointStoreDbContext(builder.Options, false);
        }
    }
}
