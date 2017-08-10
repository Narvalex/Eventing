using System;
using System.Linq;

namespace Eventing.OfflineClient.EntityFramework
{
    public class EntityFramworkPendingMessageQueue : IDurablePendingMessageQueue
    {
        private InMemoryPendingMessagesQueue cache = new InMemoryPendingMessagesQueue();
        private Func<bool, PendingMessageQueueDbContext> contextFactory;

        private object syncRoot = new object();

        public EntityFramworkPendingMessageQueue(Func<bool, PendingMessageQueueDbContext> contextFactory)
        {
            Ensure.NotNull(contextFactory, nameof(contextFactory));

            this.contextFactory = contextFactory;
            this.Initialize();
        }

        public bool DatabseExists
        {
            get
            {
                return this.ExecuteSqlAsReadOnly(context => context.Database.Exists());
            }
        }

        public void CreateDbIfNotExists()
        {
            this.ExecuteSqlAsReadOnly(context => context.Database.CreateIfNotExists());
        }

        public void DropDb()
        {
            this.ExecuteSqlAsReadOnly(context => context.Database.Delete());
        }

        public void Dequeue()
        {
            this.DoWithLock(() =>
            {
                this.ExecuteWriteInSql(ctx =>
                {
                    var entity = ctx.PendingMessageQueue.First();
                    entity.Sent = true;
                    entity.DateSent = DateTime.Now;
                });

                this.cache.Dequeue();
            });
        }

        public void Enqueue(PendingMessage message)
        {
            this.DoWithLock(() =>
            {
                var entity = new PendingMessageEntity
                {
                    Payload = message.Payload,
                    Sent = false,
                    Type = message.Type,
                    Url = message.Url,
                    DateEnqueued = DateTime.Now
                };
                this.ExecuteWriteInSql(context => context.PendingMessageQueue.Add(entity));

                this.cache.Enqueue(message);
            });
        }

        public bool TryPeek(out PendingMessage message)
        {
            return this.cache.TryPeek(out message);
        }

        private void Initialize()
        {
            this.DoWithLock(() =>
            {
                var pendingList = this.ExecuteSqlAsReadOnly(context =>
                    context.PendingMessageQueue
                           .Where(x => x.Sent == false)
                           .OrderBy(x => x.Id)
                           .ToList());

                if (pendingList.Count == 0)
                    return;

                pendingList
                .Select(x => new PendingMessage(x.Url, x.Type, x.Payload))
                .ForEach(x => this.cache.Enqueue(x));
            });
        }

        private void DoWithLock(Action doStuff)
        {
            lock (this.syncRoot)
            {
                doStuff();
            }
        }

        private void ExecuteSqlAsReadOnly(Action<PendingMessageQueueDbContext> execution)
        {
            using (var context = this.contextFactory.Invoke(true))
            {
                execution.Invoke(context);
            }
        }

        private T ExecuteSqlAsReadOnly<T>(Func<PendingMessageQueueDbContext, T> execution)
        {
            using (var context = this.contextFactory.Invoke(true))
            {
                return execution.Invoke(context);
            }
        }

        private void ExecuteWriteInSql(Action<PendingMessageQueueDbContext> execution)
        {
            using (var context = this.contextFactory.Invoke(false))
            {
                execution.Invoke(context);
                context.SaveChanges();
            }
        }
    }
}
