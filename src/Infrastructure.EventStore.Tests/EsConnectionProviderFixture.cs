using Xunit;

namespace Infrastructure.EventStore.Tests
{
    public class given_constructor
    {
        [Fact]
        public void when_not_passing_any_params_then_still_can_instantiate()
        {
            using (var sut = new EsConnectionProvider())
            {
                Assert.NotNull(sut);
            }
        }

        [Fact]
        public void when_instantiated_then_can_dispose()
        {
            using (var sut = new EsConnectionProvider())
            {
                sut.Dispose();
            }
        }
    }
}
