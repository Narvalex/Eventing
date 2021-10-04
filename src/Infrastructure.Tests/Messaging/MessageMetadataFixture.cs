using Infrastructure.Messaging;
using Infrastructure.Utils;
using System;
using Xunit;

namespace Infrastructure.Tests.Messaging
{
    public class MessageMetadataFixture
    {
        [Fact]
        public void when_passing_white_space_then_throws()
        {
            Assert.Throws<ArgumentException>(() => new MessageMetadata(" ", "name", "ip", "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", " ", "ip", "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", "name", " ", "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", "name", "ip", " "));
            Assert.Throws<ArgumentException>(() => new MessageMetadata(" ", " ", "  ", " "));
        }

        [Fact]
        public void when_passing_empty_text_then_throws()
        {
            Assert.Throws<ArgumentException>(() => new MessageMetadata("", "name", "ip", "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", "", "ip", "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", "name", "", "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", "name", "ip", ""));
            Assert.Throws<ArgumentException>(() => new MessageMetadata(string.Empty, string.Empty, string.Empty, string.Empty));
        }

        [Fact]
        public void when_passing_null_then_throws()
        {
            Assert.Throws<ArgumentException>(() => new MessageMetadata(null, "name", "ip", "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", null, "ip", "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", "name", null, "userAgent"));
            Assert.Throws<ArgumentException>(() => new MessageMetadata("authorId", "name", "ip", null));
            Assert.Throws<ArgumentException>(() => new MessageMetadata(null, null, null, null));
        }

        [Fact]
        public void when_passing_all_arguments_then_can_read_metadata()
        {
            var metadata = new MessageMetadata("authorId", "name", "ip", "userAgent");

            Assert.False(metadata.AuthorId.IsEmpty()
                         && metadata.AuthorName.IsEmpty()
                         && metadata.ClientIpAddress.IsEmpty()
                         && metadata.UserAgent.IsEmpty());
        }
    }
}
