using Infrastructure.BackupManagement;
using Infrastructure.EntityFramework.ReadModel;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.EntityFramework
{
    /// <summary>
    /// Represents a non generic database initializer. Is helpfull to 
    /// resolve all generic implementations through this contract.
    /// </summary>
    public interface IEfDbInitializer : IRelationalDbBackupCreator
    {
        void EnsureDatabaseExistsAndItsUpdated();
        void DropAndCreateDb();
        bool IsDbContextType<TDbContext>() where TDbContext : DbContext;
        string TryGetReadModelName();
        Func<ReadModelDbContext> TryGetReadModelContextFactory();
        EfDbInitializer<TDbContext> ResolveFor<TDbContext>() where TDbContext : DbContext;
    }
}
