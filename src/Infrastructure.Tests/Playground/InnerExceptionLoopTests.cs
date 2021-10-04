using Infrastructure.Logging;
using System;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class InnerExceptionLoopTests
    {
        [Fact]
        public void when_iterating_inner_exceptions_does_not_change_orignal_reference()
        {
            var exception = new Exception("1", new Exception("2", new Exception("3")));
            Assert.Equal("1", exception.Message);

            this.ExceptionsIterator(exception);

            var log = new ConsoleLogger("none", true);
            log.Error(exception, "test");

            Assert.Equal("1", exception.Message);
        }

        public void ExceptionsIterator(Exception exception)
        {
            while (exception != null)
            {
                Console.WriteLine(exception.Message);
                exception = exception.InnerException;
            }
        }
    }
}
