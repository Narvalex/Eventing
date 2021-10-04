using Infrastructure.EventStore.Messaging;
using Infrastructure.Utils;
using System;
using Xunit;

namespace Infrastructure.EventStore.Tests.Messaging
{
    public class StreamJoinProjectionScriptFixture
    {
        [Theory]
        [InlineData("stream1")]
        [InlineData("stream1", "stream2")]
        [InlineData("stream1", "stream2", "stream3")]
        public void when_passing_streams_to_generator_then_generates_script(params string[] streams)
        {
            var outputStream = "testAppEvents";
            var script = StreamJoinProjectionScript.Generate(outputStream, streams);

            Assert.NotEmpty(script);
            Assert.Contains(outputStream, script);
            streams.ForEach(x =>
            {
                Assert.Contains(x, script);
            });
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void when_passing_empty_outputStream_then_throws(string outputStream)
        {
            Assert.Throws<ArgumentException>(() => StreamJoinProjectionScript.Generate(outputStream, new string[] { "stream1" }));
        }

        [Fact]
        public void when_passing_empty_stream_list_then_throws()
        {
            Assert.Throws<ArgumentException>(() => StreamJoinProjectionScript.Generate("outputstream", null));
            Assert.Throws<ArgumentException>(() => StreamJoinProjectionScript.Generate("outputstream", new string[0]));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("", " ", null)]
        public void when_passing_empty_string_in_stream_list_then_throws(params string[] streams)
        {
            Assert.Throws<ArgumentException>(() => StreamJoinProjectionScript.Generate("outputstream", streams));
        }
    }
}
