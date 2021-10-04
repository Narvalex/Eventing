using Infrastructure.Utils;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Utils
{
    public class TaskRetryFactoryFixture
    {
        [Fact]
        public async Task when_exception_is_not_acceptable_then_throws()
        {
            await Assert.ThrowsAsync<NotImplementedException>(async () => 
            await TaskRetryFactory.Get<object>(
                () => throw new NotImplementedException(),
                ex => !(ex is NotImplementedException),
                TimeSpan.FromMilliseconds(0),
                TimeSpan.FromSeconds(5))
            );
        }

        [Fact]
        public async Task when_timeout_then_throws()
        {
            await Assert.ThrowsAsync<TimeoutException>(async () =>
            await TaskRetryFactory.Get<object>(
                () => throw new NotImplementedException(),
                ex => true,
                TimeSpan.FromMilliseconds(150),
                TimeSpan.FromMilliseconds(0))
            );
        }

        [Fact]
        public async Task when_succeeds_in_first_attempt_then_returns()
        {
            var result = await TaskRetryFactory.Get<object>(
                async () => await Task.FromResult(new object()),
                ex => false,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMilliseconds(0));

            Assert.NotNull(result);
        }

        [Fact]
        public async Task when_succeeds_in_second_attempt_then_returns()
        {
            var attempt = 0;
            var result = await TaskRetryFactory.Get<object>(
                async () =>
                {
                    attempt++;
                    if (attempt < 2)
                        throw new Exception();
                    else
                        return await Task.FromResult(new object());
                },
                ex => true,
                TimeSpan.FromMilliseconds(0),
                TimeSpan.FromSeconds(10));

            Assert.NotNull(result);
            Assert.Equal(2, attempt);
        }
    }
}
