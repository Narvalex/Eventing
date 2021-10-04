using Infrastructure.EventStore.Messaging;
using Infrastructure.Utils;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.EventStore.Tests.Messaging
{
    public abstract class given_no_projection_on_db
    {
        protected Mock<IEsProjectionClient> projClient;

        public given_no_projection_on_db()
        {
            this.projClient = new Mock<IEsProjectionClient>()
                              .Tap(c => c.Setup(x => x.GetScriptAsync(It.IsAny<string>()))
                                               .ReturnsAsync(default(string)));
        }
    }


    public class given_manager_an_no_projections : given_no_projection_on_db
    {
        private ProjectionVersionManager sut;
        private string streamName = "testAppEvents";

        public given_manager_an_no_projections()
        {
            this.sut = new ProjectionVersionManager(this.projClient.Object);
        }

        [Fact]
        public async Task when_ensuring_is_updated_then_returns_false_because_there_wasnt_any()
        {
            await this.sut.EnsureProjectionExistsAndIsUptoDate<TestAppCategoryV1>(this.streamName);
        }
    }

    public class when_ensuring_is_updated : given_no_projection_on_db
    {
        private ProjectionVersionManager sut;
        private string streamName = "testAppEvents";

        public when_ensuring_is_updated()
        {
            this.sut = new ProjectionVersionManager(this.projClient.Object);
            this.sut.EnsureProjectionExistsAndIsUptoDate<TestAppCategoryV1>(this.streamName).Wait();
        }

        [Fact]
        public void then_creates_new_continuous_projection()
        {
            this.projClient.Verify(p => p.CreateContinuousAsync(this.streamName, It.IsAny<string>()));
        }

        [Fact]
        public void then_creates_new_continuous_projection_to_outgoing_stream_and_joining_all_specified_streams()
        {
            this.projClient.Verify(p => p.CreateContinuousAsync(this.streamName, It.Is<string>(
                x =>
                    !x.IsEmpty()
                    && x.Contains(this.streamName)
                    && Ensured.AllUniqueConstStrings<TestAppCategoryV1>()
                       .All(s => x.Contains(x)))));
        }
    }

    public class given_manager_and_running_projections
    {
        private ProjectionVersionManager sut;
        private Mock<IEsProjectionClient> projClient;
        private string streamName = "testAppEvents";

        public given_manager_and_running_projections()
        {
            this.projClient = new Mock<IEsProjectionClient>()
                              .Tap(c => c.Setup(x => x.GetScriptAsync(this.streamName))
                                               .ReturnsAsync(StreamJoinProjectionScript
                                                .Generate(this.streamName,
                                                    Ensured.AllUniqueConstStrings<TestAppCategoryV1>())));

            this.sut = new ProjectionVersionManager(this.projClient.Object);

        }

        [Fact]
        public async Task when_ensuring_is_updated_with_same_streams_then_returns_true_because_is_already_updated()
        {
            await this.sut.EnsureProjectionExistsAndIsUptoDate<TestAppCategoryV1>(this.streamName);
        }

        [Fact]
        public async Task when_ensuring_is_updated_with_same_streams_then_does_not_try_to_update_db()
        {
            await this.sut.EnsureProjectionExistsAndIsUptoDate<TestAppCategoryV1>(this.streamName);
            this.projClient.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task when_ensuring_is_updated_with_same_streams_then_does_not_try_to_create_either()
        {
            await this.sut.EnsureProjectionExistsAndIsUptoDate<TestAppCategoryV1>(this.streamName);
            this.projClient.Verify(x => x.CreateContinuousAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task when_ensuring_with_new_streams_then_updates()
        {
            await this.sut.EnsureProjectionExistsAndIsUptoDate<TestAppCategoryV2>(this.streamName);
            this.projClient.Verify(x => x.UpdateAsync(this.streamName, It.IsAny<string>()));
        }

        [Fact]
        public async Task when_ensuring_with_new_streams_then_returns_false()
        {
            await this.sut.EnsureProjectionExistsAndIsUptoDate<TestAppCategoryV2>(this.streamName);
        }

        [Fact]
        public async Task when_ensuring_with_new_streams_then_updates_the_projection_with_all_new_streams()
        {
            await this.sut.EnsureProjectionExistsAndIsUptoDate<TestAppCategoryV2>(this.streamName);
            this.projClient.Verify(p => p.UpdateAsync(this.streamName, It.Is<string>(
                x =>
                    !x.IsEmpty()
                    && x.Contains(this.streamName)
                    && x.Contains(TestAppCategoryV2.OrgsV2)
                    && Ensured.AllUniqueConstStrings<TestAppCategoryV2>()
                       .All(s => x.Contains(x)))));
        }
    }
}
