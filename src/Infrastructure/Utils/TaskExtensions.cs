using System;
using System.Threading.Tasks;

namespace Infrastructure.Utils
{
    public static class TaskExtensions
    {
        public static async Task<TNewResult> Then<TResult, TNewResult>(this Task<TResult> task, Func<TResult, TNewResult> continuationFunction)
        {
            var result = await task;
            return continuationFunction(result);
        }

        public static async Task<TNewResult> ThenAsync<TResult, TNewResult>(this Task<TResult> task, Func<TResult, Task<TNewResult>> continuationFunction)
        {
            var result = await task;
            return await continuationFunction(result);
        }
    }
}
