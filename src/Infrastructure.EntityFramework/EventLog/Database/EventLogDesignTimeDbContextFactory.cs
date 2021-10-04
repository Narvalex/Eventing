using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.EntityFramework.EventLog
{
    public class EventLogDesignTimeDbContextFactory : IDesignTimeDbContextFactory<EventLogDbContext>
    {
        public EventLogDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<EventLogDbContext>();
            builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=EventStoreDbContext;Trusted_Connection=True;MultipleActiveResultSets=true");
            return new EventLogDbContext(builder.Options, false);
        }
    }
}
