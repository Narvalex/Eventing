using Infrastructure.EventSourcing;
using Infrastructure.Messaging.Handling;
using Xunit;

namespace Infrastructure.Tests.Messaging.Handling
{
    public class given_empty_volatile_checkpoint_repository
    {
        private VolatileCheckpointStore sut = new VolatileCheckpointStore();

        [Fact]
        public void when_getting_checkpoint_then_gets_no_version_number()
        {
            var checkpoint = this.sut.GetCheckpoint(new EventProcessorId("elasitcsearchReadModelGen", EventProcessorConsts.ReadModelProjection));
            Assert.Equal(Checkpoint.Start, checkpoint);
        }

        [Fact]
        public void when_updating_checkpoint_then_gets_updated()
        {
            var subName = new EventProcessorId("elasitcsearchReadModelGen", EventProcessorConsts.ReadModelProjection);

            Assert.Equal(Checkpoint.Start, this.sut.GetCheckpoint(subName));

            this.sut.CreateOrUpdate(subName, new Checkpoint(new EventPosition(0, 0), 0));

            Assert.Equal(new Checkpoint(new EventPosition(0, 0), 0), this.sut.GetCheckpoint(subName));
        }

        [Fact]
        public void when_updating_twice_then_gets_last_value()
        {
            var subName = new EventProcessorId("elasitcsearchReadModelGen", EventProcessorConsts.ReadModelProjection);

            Assert.Equal(Checkpoint.Start, this.sut.GetCheckpoint(subName));

            this.sut.CreateOrUpdate(subName, new Checkpoint(new EventPosition(0, 0), 0));

            Assert.Equal(new Checkpoint(new EventPosition(0, 0), 0), this.sut.GetCheckpoint(subName));

            this.sut.CreateOrUpdate(subName, new Checkpoint(new EventPosition(1, 1), 1));

            Assert.Equal(new Checkpoint(new EventPosition(1, 1), 1), this.sut.GetCheckpoint(subName));
        }
    }
}
