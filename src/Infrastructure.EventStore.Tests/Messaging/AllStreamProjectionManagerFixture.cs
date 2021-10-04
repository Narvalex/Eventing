using Infrastructure.EventStore.Messaging;
using Infrastructure.Utils;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.EventStore.Tests.Messaging
{
    public class given_no_projection
    {
        protected Mock<IEsProjectionClient> projClient;
        private AllStreamProjectionManager sut;

        public given_no_projection()
        {
            this.projClient = new Mock<IEsProjectionClient>()
                             .Tap(c => c.Setup(x => x.GetScriptAsync(It.IsAny<string>()))
                                              .ReturnsAsync(default(string)));
            this.sut = new AllStreamProjectionManager(this.projClient.Object);
        }

        [Fact]
        public async Task when_ensuring_is_created_then_returns_false_because_it_was_not_found()
        {
            Assert.False(await this.sut.EnsureIsCreatedAndIsRunning());
        }

        [Fact]
        public async Task when_ensuring_is_created_then_posts_script()
        {
            await this.sut.EnsureIsCreatedAndIsRunning();

            this.projClient.Verify(p => p.CreateContinuousAsync(AllStreamProjection.EmittedStreamName, AllStreamProjection.Script));
        }
    }

    public class given_all_stream_projection_in_db
    {
        protected Mock<IEsProjectionClient> projClient;
        private AllStreamProjectionManager sut;

        public given_all_stream_projection_in_db()
        {

            this.projClient = new Mock<IEsProjectionClient>()
                             .Tap(c => c.Setup(x => x.GetScriptAsync(It.IsAny<string>()))
                                              .ReturnsAsync(AllStreamProjection.Script));

            this.sut = new AllStreamProjectionManager(this.projClient.Object);
        }

        [Fact]
        public async Task when_ensuring_is_created_then_returns_true_because_it_was_found()
        {
            Assert.True(await this.sut.EnsureIsCreatedAndIsRunning());
        }

        [Fact]
        public void when_ensuring_is_created_then_does_not_post_nothing_at_all()
        {
            this.projClient.Verify(p => p.CreateContinuousAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    public class given_wrong_all_stream_projection_in_db
    {
        protected Mock<IEsProjectionClient> projClient;
        private AllStreamProjectionManager sut;

        public given_wrong_all_stream_projection_in_db()
        {
            this.projClient = new Mock<IEsProjectionClient>()
                             .Tap(c => c.Setup(x => x.GetScriptAsync(It.IsAny<string>()))
                                              .ReturnsAsync(AllStreamProjection.Script + "Injected Script here"));

            this.sut = new AllStreamProjectionManager(this.projClient.Object);
        }

        [Fact]
        public async Task when_ensuring_is_created_then_throws()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await this.sut.EnsureIsCreatedAndIsRunning());
        }
    }
}
