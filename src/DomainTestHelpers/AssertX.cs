using Infrastructure.Messaging;
using Xunit;

namespace Erp.Domain.Tests.Helpers
{
    /// <summary>
    /// Assert extensions
    /// </summary>
    public class AssertX
    {
        protected AssertX()
        {

        }

        public static void Equal(string expected, string actualFirst, string actualSeccond)
        {
            Assert.Equal(expected, actualFirst);
            Assert.Equal(expected, actualSeccond);
        }

        /// <summary>
        /// Verifica que el stream id sea igual a la propiedad del evento y estos igual al esperado.
        /// </summary>
        public static void Equal(string expectedStreamId, string expectedEventProperty, IEvent @event)
            => Equal(expectedStreamId, expectedEventProperty, @event.StreamId);
    }
}
