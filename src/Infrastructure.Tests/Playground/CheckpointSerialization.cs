using Infrastructure.Messaging.Handling;
using Infrastructure.Serialization;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class CheckpointSerialization
    {
        private IJsonSerializer sut = new NewtonsoftJsonSerializer();

        [Fact]
        public void can_serialize_checkpoint_struct()
        {
            var c1 = new Checkpoint(new EventPosition(1, 1), 3);
            var serialized = this.sut.Serialize(c1);
            var c2 = this.sut.Deserialize<Checkpoint>(serialized);

            Assert.Equal(c1, c2);
        }
    }
}
