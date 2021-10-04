using Infrastructure.DateTimeProvider;
using Infrastructure.EntityFramework.EventStorage;
using Infrastructure.EntityFramework.EventStorage.Database;
using Infrastructure.EventSourcing;
using Infrastructure.EventSourcing.Transactions;
using Infrastructure.Messaging;
using Infrastructure.Processing.WriteLock;
using Infrastructure.Serialization;
using Infrastructure.Snapshotting;
using Infrastructure.Tests.EventSourcing;
using Infrastructure.Tests.Transactions.Helpers;
using Infrastructure.Utils;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Transactions
{
    public class givenTransaction
    {
        private EmployeeCmdHandler cmdHandler;
        private TransactionCrashRecoveryEvHandler sut;
        private EsRepsitoryDecorated repo;
        private IOnlineTransactionPool txPool;

        private string orgId = "79";
        public givenTransaction()
        {
            var dbName = Guid.NewGuid().ToString();
            var validNamespace = "Infrastructure.Tests.EventSourcing";
            EventSourced.SetValidNamespace(validNamespace);
            var serializer = new NewtonsoftJsonSerializer();
            var dateTime = new LocalDateTimeProvider();
            var versionManager = new EventDeserializationAndVersionManager(serializer, validNamespace, "Infrastructure.Tests");
            var eventStore = new EfEventStore(() => EventStoreDbContext.ResolveNewInMemoryContext(dbName), serializer,
              versionManager, dateTime);
            var cache = new SnapshotRepository(new NoExclusiveWriteLock(), serializer, new NoopPersistentSnapshotter(), TimeSpan.FromMinutes(30));
            this.txPool = new OnlineTransactionPool();
            this.repo = new EsRepsitoryDecorated(new EventSourcedRepository(eventStore, cache, dateTime, this.txPool, serializer, versionManager));
            this.cmdHandler = new EmployeeCmdHandler(repo);
            this.sut = new TransactionCrashRecoveryEvHandler(this.repo, versionManager);

            // Arrange
            var cmd = new CreateOrganization(this.orgId);
            ((ICommandInTransit)cmd).SetMetadata(new MessageMetadata("test", "test", "localhost", "Opera"));

            // Act
            this.cmdHandler.Handle(cmd).Wait();
        }


        [Fact]
        public async Task closed_then_recoverer_ignores()
        {
            // Arrange
            var id = "jperez";
            var name = "John Perez";
            var cmd = new CreateEmployee(this.orgId, id, name);
            ((ICommandInTransit)cmd).SetMetadata(new MessageMetadata("test", "test", "localhost", "Opera"));

            // Act
            await this.cmdHandler.Handle(cmd);
            var e = new NewTransactionPrepareStarted(this.repo.Transaction.TransactionId);
            await this.sut.Handle(e);
        }

        [Fact]
        public async Task in_prepare_phase_when_system_crashes_then_rolls_back()
        {
            // Arrange
            var id = "jperez";
            var name = "John Perez";
            var cmd = new CreateEmployee(this.orgId, id, name);
            ((ICommandInTransit)cmd).SetMetadata(new MessageMetadata("test", "test", "localhost", "Opera"));

            // Crash
            await this.cmdHandler.PrepareButNotCommit(cmd);
            this.txPool.Unregister(this.repo.Transaction.TransactionId);

            // Recovery process
            //----------------------------------------------------------------------------------------------------------
            // Prepare Started
            var e1 = new NewTransactionPrepareStarted(this.repo.Transaction.TransactionId);
            ((IEventInTransit)e1).SetEventMetadata(
                new EventMetadata(Guid.NewGuid(), "corr", "caus", "5556", DateTime.Now, "i", "me", "localhost", "IE6"), 
                typeof(NewTransactionPrepareStarted).Name.WithFirstCharInLower());
            await this.sut.Handle(e1);

            // Lock Granted
            var e2 = new LockAcquired(this.orgId, this.repo.Transaction.TransactionId);
            ((IEventInTransit)e2).SetEventMetadata(
                new EventMetadata(Guid.NewGuid(), "corr", "caus", "5556", DateTime.Now, "i", "me", "localhost", "IE6")
                .Tap(x => x.SetEventSourcedTypeUnsafe(typeof(Organization).Name.WithFirstCharInLower())),
                 typeof(LockAcquired).Name.WithFirstCharInLower());
            await this.sut.Handle(e2);
        }
    }
}
