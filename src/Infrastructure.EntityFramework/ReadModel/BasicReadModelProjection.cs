using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{
    public abstract class BasicReadModelProjection<T> : IReadModelProjection, IDisposable where T : ReadModelDbContext
    {
        protected readonly IEfReadModelProjector<T> efReadModelProjector;

        public BasicReadModelProjection(IEfReadModelProjector<T> efReadModelProjector)
        {
            this.efReadModelProjector = efReadModelProjector.EnsuredNotNull(nameof(efReadModelProjector));
        }

        public string ReadModelName => this.efReadModelProjector.ReadModelName;

        public bool BatchWritesAreEnabled => this.efReadModelProjector.BatchWritesAreEnabled;

        public bool IsNowBatchWriting => this.efReadModelProjector.IsBatchWritingNow;

        public void Dispose()
        {
            using (this.efReadModelProjector as IDisposable)
            {
                // Dispose projector if applicable
            }
        }

        void IEventHandler.NotifyLiveProcessingStarted()
        {
            this.efReadModelProjector.LiveProcessingStarted();
        }

        protected virtual Task Reduce(IEvent e, Func<T, Task> function) =>
            this.efReadModelProjector.Project(e, function);

        public void OnBatchCommited(Action<Checkpoint> checkpoint) => this.efReadModelProjector.OnBatchCommited(checkpoint);
    }
}
