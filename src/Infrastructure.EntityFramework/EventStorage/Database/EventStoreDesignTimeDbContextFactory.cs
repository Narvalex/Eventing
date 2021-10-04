using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.EntityFramework.EventStorage.Database
{
    public class EventStoreDesignTimeDbContextFactory : IDesignTimeDbContextFactory<EventStoreDbContext>
    {
        public EventStoreDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<EventStoreDbContext>();
            builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=EventStoreDbContext;Trusted_Connection=True;MultipleActiveResultSets=true");
            return new EventStoreDbContext(builder.Options, false);
        }
    }
}
