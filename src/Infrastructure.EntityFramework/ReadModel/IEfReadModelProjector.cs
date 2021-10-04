using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{
    public interface IEfReadModelProjector<T> where T : ReadModelDbContext
    {
        string ReadModelName { get; }

        Task Project(IEvent e, Func<T, Task> projection);

        void LiveProcessingStarted();

        bool BatchWritesAreEnabled { get; }

        bool IsBatchWritingNow { get; }

        void OnBatchCommited(Action<Checkpoint> checkpoint);

        // For convinience -----------------

        Func<T> DbContextFactory { get; }

        IEventStore EventStore { get; }
    }
}