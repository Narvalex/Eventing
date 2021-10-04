using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class CoallesenceOperator
    {
        [Fact]
        public void false_clause_not_executed_Test()
        {
            var hello = new object();
            var anotherHello = hello ?? ExceptionThrower();
            Assert.NotNull(anotherHello);
        }

        private object ExceptionThrower()
        {
            Assert.True(false);
            return "hi";
        }
    }
}
