using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.EntityFramework.Files
{
    public class FilesDesignTimeDbContextFactory : IDesignTimeDbContextFactory<FilesDbContext>
    {
        public FilesDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<FilesDbContext>();
            builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Files;Trusted_Connection=True;MultipleActiveResultSets=true");
            return new FilesDbContext(builder.Options, false);
        }
    }
}
