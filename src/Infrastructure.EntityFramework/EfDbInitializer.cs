using Infrastructure.BackupManagement;
using Infrastructure.EntityFramework.EventLog;
using Infrastructure.EntityFramework.EventStorage.Database;
using Infrastructure.EntityFramework.Files;
using Infrastructure.EntityFramework.Messaging.Handling.Database;
using Infrastructure.EntityFramework.ReadModel;
using Infrastructure.EntityFramework.Snapshotting.Database;
using Infrastructure.Logging;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework
{
    public class EfDbInitializer<T> : IEfDbInitializer where T : DbContext
    {
        private readonly Func<DbContextOptions<T>, bool, T> contextFactory;
        private readonly ILogLite log = LogManager.GetLoggerFor<EfDbInitializer<T>>();
        private Type type = typeof(T);
        private readonly TimeSpan backupAndRestoreTimeout = TimeSpan.FromHours(3);
        private readonly string connectionString;
        private readonly string? readModelName;

        public EfDbInitializer() : this(Guid.NewGuid().ToString(), true, "", true)
        { }

        public EfDbInitializer(string basicConnectionString, bool isDevEnv, string prefix = "", bool inMemory = false)
        {
            Ensure.NotEmpty(basicConnectionString, nameof(basicConnectionString));

            var builder = new SqlConnectionStringBuilder(inMemory ? "" : basicConnectionString); // InMemory does not create connstrings
            builder.MultipleActiveResultSets = true;
            if (isDevEnv)
                builder.TrustServerCertificate = true;
            builder.ConnectTimeout = 60 * 2; // 2 minutes

            RelationalDbType? relDbType = null;
            string? dbName = null;
            if (this.type == typeof(CheckpointStoreDbContext))
            {
                relDbType = RelationalDbType.CheckpointStore;
                dbName = "CheckpointStore";
            }
            else if (this.type == typeof(SnapshotStoreDbContext))
            {
                relDbType = RelationalDbType.SnapshotStore;
                dbName = "SnapshotStore";
            }
            else if (this.type == typeof(FilesDbContext))
            {
                relDbType = RelationalDbType.Files;
                dbName = "Files";
            }
            else if (this.type == typeof(EventStoreDbContext))
            {
                relDbType = RelationalDbType.EventStore;
                dbName = "EventStore";
            }
            else if (this.type == typeof(EventLogDbContext))
            {
                relDbType = RelationalDbType.EventLog;
                dbName = "EventLog";
            }

            if (relDbType is not null && dbName is not null)
            {
                // Fixed single db
                this.RelationalDbType = relDbType.Value;
                this.VirtualDbName = dbName;
                this.RealDbName = this.VirtualDbName;
                this.Prefix = string.Empty;

                builder.InitialCatalog = this.RealDbName;
            }
            else
            {
                /* IMPORTANT: 
                / Every other db will try to be recongnized as ReadModel
                * Why we separate read models in green and blue databases?
                * The reason is to have ZERO DOWNTIME when rebuilding a ReadModel.
                */
                if (!this.type.Name.EndsWith("ReadModelDbContext"))
                    throw new InvalidOperationException($"The {this.type.Name} DbContext name is not valid. The DbContextName should end with 'ReadModelDbContext'");

                this.RelationalDbType = RelationalDbType.ReadModel;
                this.VirtualDbName = $"ReadModel_{this.type.Name.Split("ReadModelDbContext").First()}";
                this.Prefix = prefix.NotEmpty() ? $"{prefix}_" : "";
                this.RealDbName = $"{this.Prefix}{this.VirtualDbName}";

                builder.InitialCatalog = this.RealDbName;

                this.readModelName = ReadModelDbContext.ResolveReadModelName(this.type);
            }

            // No more basic here thouhg
            this.connectionString = inMemory ? basicConnectionString : builder.ConnectionString;
            this.log.Verbose("ConnectionString created: " + this.connectionString);

            // Resolve Context factories
            // Source for original: https://github.com/microsoftarchive/cqrs-journey/blob/master/source/Infrastructure/Azure/Infrastructure.Azure/EventSourcing/AzureEventSourcedRepository.cs
            // Precompiled Lambda: https://stackoverflow.com/questions/55137619/pre-compiled-lambda-expression-to-create-class-that-has-a-constructor-with-a-par

            var constructor = typeof(T).GetConstructor(new[] { typeof(DbContextOptions<T>), typeof(bool) });
            if (constructor is null)
                throw new InvalidCastException(
                    "Type T must have a constructor with the following signature: .ctor(DbContextOptions<T>, bool)");

            var optionsParameter = Expression.Parameter(typeof(DbContextOptions<T>));
            var detectChangesParameter = Expression.Parameter(typeof(bool));

            var lambda = Expression.Lambda<Func<DbContextOptions<T>, bool, T>>(
                            Expression.New(
                                constructor,
                                new Expression[] { optionsParameter, detectChangesParameter }),
                            optionsParameter, detectChangesParameter);

            this.contextFactory = lambda.Compile();

            if (inMemory)
            {
                var options = new DbContextOptionsBuilder<T>()
                   .UseInMemoryDatabase(this.connectionString) //database name in this case
                   .Options;

                this.ResolveReadContext = () => contextFactory(options, false);
                this.ResolveWriteContext = () => contextFactory(options, true);
                this.ResolveWriteContextWithoutLazyLoading = () => contextFactory(options, true);
            }
            else
            {
                // Lazy loading...
                var options = new DbContextOptionsBuilder<T>()
                                .UseLazyLoadingProxies()
                                .UseSqlServer(this.connectionString)
                                .ConfigureWarnings(builder =>
                                {
                                    builder.Ignore(CoreEventId.LazyLoadOnDisposedContextWarning);
                                })
                                .Options;

                this.ResolveReadContext = () => contextFactory(options, false);
                this.ResolveWriteContext = () => contextFactory(options, true);

                var optionsWithoutLazy = new DbContextOptionsBuilder<T>()
                                            .UseSqlServer(this.connectionString)
                                            .Options;

                this.ResolveWriteContextWithoutLazyLoading = () => contextFactory(optionsWithoutLazy, true);
            }
        }

        public void EnsureDatabaseExistsAndItsUpdated()
        {
            var contextName = typeof(T).Name;
            this.log.Info($"Checking the sql database for {contextName}. If not exists a new one will be created.");
            using (var context = this.ResolveReadContext.Invoke())
            {
                if (!((RelationalDatabaseCreator)context.GetService<IDatabaseCreator>()).Exists())
                {
                    this.log.Info($"The sql database for {contextName} was not found. Creating a new one...");
                    this.ApplyMigrationsIfApplicable(contextName, context);
                    context.Database.EnsureCreated();
                    this.log.Info($"The database for {contextName} was created successfully!");
                }
                else
                {
                    this.log.Info($"The database for {contextName} was found!");
                    this.ApplyMigrationsIfApplicable(contextName, context);
                    this.log.Info($"Sql database for {contextName} is ready");
                }
            }
        }

        public void DropAndCreateDb()
        {
            var contextName = typeof(T).Name;
            this.log.Warning($"Checking the sql database for {contextName}. If exists it will be DROPED and then created again. I sure hope you know what you are doing...");
            using (var context = this.ResolveReadContext.Invoke())
            {
                if (((RelationalDatabaseCreator)context.GetService<IDatabaseCreator>()).Exists())
                {
                    this.log.Info($"The sql database for {contextName} was found. Deleting the current db...");
                    context.Database.EnsureDeleted();
                    this.log.Info($"The sql database for {contextName} was successfully deleted!");
                }

                this.EnsureDatabaseExistsAndItsUpdated();
            }
        }

        private void ApplyMigrationsIfApplicable(string contextName, DbContext context)
        {
            this.log.Info($"Getting pending migrations for {contextName}...");
            var migrations = context.Database.GetPendingMigrations();
            var count = migrations.Count();
            this.log.Info($"{count} pending migrations found in {contextName}");
            migrations.ToList().ForEach(x => this.log.Info(x));
            if (count > 0)
            {
                this.log.Info($"Applying pending migrations to {contextName}...");
                context.Database.Migrate();
                this.log.Info($"Migrations applied successfully to {contextName}!");
            }
        }

        public RelationalDbType RelationalDbType { get; }

        public Func<T> ResolveWriteContextWithoutLazyLoading { get; }

        public Func<T> ResolveWriteContext { get; }

        public Func<T> ResolveReadContext { get; }

        public string VirtualDbName { get; }
        public string RealDbName { get; }
        public string Prefix { get; }

        public string TryGetReadModelName()
        {
            if (this.readModelName.IsEmpty())
                throw new InvalidOperationException("Not a read model");
            return this.readModelName!;
        }

        public async Task CreateBackupToDestination(string destinationPath)
        {
            using (var context = this.ResolveReadContext())
            {
                context.Database.SetCommandTimeout(this.backupAndRestoreTimeout);
                await context.Database.ExecuteSqlRawAsync($@"
USE [master]
BACKUP DATABASE [{this.RealDbName}] TO  DISK = N'{destinationPath}\{this.VirtualDbName}.bak' WITH NOFORMAT, NOINIT,  NAME = N'{this.VirtualDbName}', NOSKIP, REWIND, NOUNLOAD,  STATS = 10
");
            }
        }

        public bool IsDbContextType<TDbContext>() where TDbContext : DbContext =>
            typeof(TDbContext) == this.type;

        public EfDbInitializer<TDbContext> ResolveFor<TDbContext>() where TDbContext : DbContext
        {
            if (!IsDbContextType<TDbContext>())
                throw new InvalidOperationException("Cast not allowed. Not the same DbContext");

            return (this as EfDbInitializer<TDbContext>)!;
        }

        public async Task RestoreBackupToDestination(string sourcePath)
        {
            var originalBackupDabaseName = string.Empty;
            var originalBackupLogName = string.Empty;
            var currentDbPath = string.Empty;
            var currentLogPath = string.Empty;

            using (var sqlConnection = new SqlConnection(this.connectionString))
            {
                await sqlConnection.OpenAsync();

                using (var sqlCommand = new SqlCommand($@"
RESTORE FILELISTONLY FROM DISK = N'{sourcePath}'
", sqlConnection))
                {
                    using (var reader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            originalBackupDabaseName = reader.GetString("LogicalName");
                        else
                            throw new InvalidOperationException("Could not read database name");

                        if (await reader.ReadAsync())
                            originalBackupLogName = reader.GetString("LogicalName");
                        else
                            throw new InvalidOperationException("Could not read database name");
                    }

                }

                using (var sqlCommand = new SqlCommand("SELECT name, physical_name FROM sys.database_files", sqlConnection))
                {
                    using (var reader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            currentDbPath = reader.GetString("physical_name");
                        else
                            throw new InvalidOperationException("Could not read path for current db");

                        if (await reader.ReadAsync())
                            currentLogPath = reader.GetString("physical_name");
                        else
                            throw new InvalidOperationException("Could not read path fro current log");
                    }

                }

                using (var sqlCommand = new SqlCommand($@"
USE[master]
ALTER DATABASE[{ this.RealDbName }] SET SINGLE_USER WITH ROLLBACK IMMEDIATE

--No need to drop...
--DROP DATABASE IF EXISTS [{ this.RealDbName }]

RESTORE DATABASE[{this.RealDbName}] FROM DISK = N'{sourcePath}' WITH FILE = 1,
MOVE N'{originalBackupDabaseName}' TO N'{currentDbPath}',  
MOVE N'{originalBackupLogName}' TO N'{currentLogPath}',  
NOUNLOAD,  
REPLACE,  
STATS = 5

ALTER DATABASE[{ this.RealDbName}] SET MULTI_USER
", sqlConnection))
                {
                    sqlCommand.CommandTimeout = Convert.ToInt32(this.backupAndRestoreTimeout.TotalSeconds);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public Func<ReadModelDbContext> TryGetReadModelContextFactory()
        {
            this.TryGetReadModelName(); // Check if falid

            return ReadModelDbContextFactory;
        }

        private ReadModelDbContext ReadModelDbContextFactory()
        {
            var context = this.ResolveWriteContext();
            return (context as ReadModelDbContext)!;
        }
    }
}
