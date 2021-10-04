using Infrastructure.Messaging;
using System;
using Xunit;

namespace Infrastructure.Tests.Messaging
{
    public class EventMetadataFixture
    {
        [Fact]
        public void when_passing_all_parameters_then_intantiates()
        {
            var metadata = new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now, "author", "name", "ip", "user_agent");

            Assert.NotNull(metadata);
        }
#if DEBUG

        [Fact]
        public void when_passing_default_event_id_then_throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventMetadata(
                default(Guid), "corrId", "causId", "commitId", DateTime.Now, "author", "name", "ip", "userAgent"));
        }

        [Fact]
        public void when_passing_empty_event_id_then_throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventMetadata(
                Guid.Empty, "corrId", "causId", "commitId", DateTime.Now, "author", "name", "ip", "userAgent"));
        }

        [Fact]
        public void when_passing_default_dateTime_then_throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EventMetadata(
                Guid.NewGuid(), "corrId", "causId", "commitId", default(DateTime),
                 "author", "name", "ip", "userAgent"));
        }

        [Fact]
        public void when_passing_empty_string_then_throws()
        {
            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "", "causId", "commitId", DateTime.Now, 
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "", "commitId", DateTime.Now,
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "", DateTime.Now,
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", "", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", "name", "", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", "name", "ip", ""));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "", "", "", DateTime.Now, "", "", "", ""));
        }

        [Fact]
        public void when_passing_white_space_string_then_throws()
        {
            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), " ", "causId", "commitId", DateTime.Now,
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", " ", "commitId", DateTime.Now,
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", " ", DateTime.Now,
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 " ", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", " ", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", "name", " ", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", "name", "ip", " "));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), " ", " ", " ", DateTime.Now, " ", " ", " ", " "));
        }

        [Fact]
        public void when_passing_null_string_then_throws()
        {
            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), null, "causId", "commitId", DateTime.Now,
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", null, "commitId", DateTime.Now,
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", null, DateTime.Now,
                 "author", "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 null, "name", "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", null, "ip", "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", "name", null, "userAgent"));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), "corrId", "causId", "commitId", DateTime.Now,
                 "author", "name", "ip", null));

            Assert.Throws<ArgumentException>(
                () => new EventMetadata(Guid.NewGuid(), null, null, null, DateTime.Now, null, null, null, null, null, null, null));
        }
#endif
    }
}
