using Infrastructure.EventSourcing;
using Infrastructure.Utils;
using System;
using System.Threading.Tasks;

namespace Infrastructure.IdGeneration
{
    public class SequentialIdFinder<T> : ISequentialIdFinder<T>
    {
        private readonly IEventSourcedReader reader;
        private readonly bool retryBackwards; // This is usefull when you have custom string ids like: userGroup-admin, userGroup-1, userGroup-2
        private readonly int maxRetries;

        public SequentialIdFinder(IEventSourcedReader reader, bool retryBackwards = false, int maxRetries = 1000)
        {
            this.reader = Ensured.NotNull(reader, nameof(reader));
            this.retryBackwards = retryBackwards;
            this.maxRetries = maxRetries;
        }

        public async Task<string> NextAsync()
        {
            var tryCount = 0;
            var (success, id) = await this.TryGetNextId(tryCount);
            if (success)
                return id;

            if (!this.retryBackwards)
                throw new ArgumentException($"The event sourced type of {typeof(T).Name} does not hold valid number ids");

            do
            {
                tryCount += 1;
                (success, id) = await this.TryGetNextId(tryCount);
                if (success)
                    return id;

            } while (tryCount < maxRetries);

            throw new ArgumentException($"Could not find a vaild number id for {typeof(T).Name} .Max retries reached: {this.maxRetries}.");
        }

        private async Task<(bool, string)> TryGetNextId(int offset)
        {
            var lastId = await this.reader.GetLastEventSourcedId<T>(offset);

            if (lastId is null)
                return (true, 1.ToString());

            if (int.TryParse(lastId, out var number))
                return (true, (number + 1).ToString());
            else
                return (false, string.Empty);
        }
    }
}
