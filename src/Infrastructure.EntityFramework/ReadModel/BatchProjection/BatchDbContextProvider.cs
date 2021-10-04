using Infrastructure.Utils;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{
    internal class BatchDbContextProvider<T> : IDisposable where T : ReadModelDbContext
    {
        private readonly Func<T> contextFactory;
        private T? dbContext;

        internal BatchDbContextProvider(Func<T> contextFactory)
        {
            this.contextFactory = Ensured.NotNull(contextFactory, nameof(contextFactory));
        }

        internal T ResolveDbContext() => dbContext ?? this.CreateDbContext();

        public void Dispose()
        {
            using (this.dbContext)
            {

            }
        }

        internal void DiscardDbContext()
        {
            using (this.dbContext)
            { }
            this.dbContext = null;
        }

        internal async Task<int> SafeSaveChangesAsync()
        {
            var result = 0;
            using (this.dbContext)
            {
                if (this.dbContext != null)
                    result = await this.dbContext.SafeSaveChangesAsync();
            }

            this.dbContext = null;
            return result;
        }

        private T CreateDbContext()
        {
            this.dbContext = this.contextFactory.Invoke();
            return this.dbContext;
        }
    }
}
