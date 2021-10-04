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
using System;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Transactions
{
    public class givenAnOrganization
    {
        private EmployeeCmdHandler sut;
        private EsRepsitoryDecorated repo;

        private string orgId = "79";

        public givenAnOrganization()
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
            this.repo = new EsRepsitoryDecorated( new EventSourcedRepository(eventStore, cache, dateTime, new OnlineTransactionPool(), serializer, versionManager));
            this.sut = new EmployeeCmdHandler(repo);

            // Arrange
            var cmd = new CreateOrganization(this.orgId);
            ((ICommandInTransit)cmd).SetMetadata(new MessageMetadata("test", "test", "localhost", "Opera"));

            // Act
            this.sut.Handle(cmd).Wait();
        }

        [Fact]
        public async Task when_handling_a_transaction_successfully_then_commit_all_events()
        {
            // Arrange
            var id = "jperez";
            var name = "John Perez";
            var cmd = new CreateEmployee(this.orgId, id, name);
            ((ICommandInTransit)cmd).SetMetadata(new MessageMetadata("test", "test", "localhost", "Opera"));

            // Act
            await this.sut.Handle(cmd);

            // Assert (TODO: Assert all entities participating in tx.)
            var contactStreamName = EventStream.GetStreamName<Contact>(id);
            var contact = await this.repo.TryGetByStreamNameEvenIfDoesNotExistsAsync<Contact>(contactStreamName);
            Assert.True(contact.Metadata.Exists);
            Assert.False(contact.Metadata.IsLocked);

            var contactEntityInTransaction = await this.repo.TryGetByIdAsync<EntityTransactionPreparation>(contactStreamName);
            Assert.NotNull(contactEntityInTransaction);
            Assert.False(contactEntityInTransaction!.PreparedEventBatches.Any());

            // TODO: Assert TxRecord
        }

        [Fact]
        public async Task when_entity_rejects_prepare_then_transaction_aborts_and_rolls_back_successfully()
        {
            // Arrange
            var id = "jperez";
            var cmd = new CreateEmployee(this.orgId, id, null!);
            ((ICommandInTransit)cmd).SetMetadata(new MessageMetadata("test", "test", "localhost", "Opera"));

            // Act
            await this.sut.Handle(cmd);

            // Assert
            var txRecord = await this.repo.GetByIdAsync<TransactionRecord>(this.repo.Transaction.TransactionId);
            Assert.Equal(TransactionStatus.Closed, txRecord.Status);
            Assert.Equal(TransactionRunMode.Online, txRecord.RunMode);
            Assert.Equal(TransactionOutcome.Aborted, txRecord.Outcome);

            // TODO: more asserts
        }
    }
}
