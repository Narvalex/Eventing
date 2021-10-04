using Infrastructure.EntityFramework.Messaging.Handling;
using System;
using System.Threading;
using Xunit;

namespace Infrastructure.EntityFramework.Tests.Messaging.Handling
{
    public class given_live_processing_handler
    {
        [Fact]
        public void when_trying_first_time_then_enters_live_processing()
        {
            var signal = new AutoResetEvent(false);
            var sut = new LiveProcessingHandler(() => signal.Set());
            sut.EnterLiveProcessingIfApplicable(0);

            Assert.True(signal.WaitOne(0));
        }
    }

    public class given_handler_already_going_live : IDisposable
    {
        private LiveProcessingHandler sut;
        private AutoResetEvent signal;
        private int threshold = 15;
        private long lastCheckpoint = 1587;

        public given_handler_already_going_live()
        {
            this.signal = new AutoResetEvent(false);
            this.sut = new LiveProcessingHandler(() => this.signal.Set(), this.threshold);
            this.sut.EnterLiveProcessingIfApplicable(this.lastCheckpoint);
            this.signal.WaitOne(0);
        }

        public void Dispose()
        {
            this.signal?.Dispose();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(53)]
        [InlineData(180)]
        public void when_trying_behind_treshold_then_does_not_reenter_live(long behindThreshold)
        {
            this.sut.EnterLiveProcessingIfApplicable(this.lastCheckpoint + (this.threshold - behindThreshold));
            Assert.False(this.signal.WaitOne(0));
        }

        [Fact]
        public void when_hitting_threshold_then_does_not_reenter_live()
        {
            this.sut.EnterLiveProcessingIfApplicable(this.lastCheckpoint + this.threshold);
            Assert.False(this.signal.WaitOne(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(100)]
        public void when_going_beyond_theshold_then_reenters_live(long beyondThreshold)
        {
            this.sut.EnterLiveProcessingIfApplicable(this.lastCheckpoint + (this.threshold + beyondThreshold));
            Assert.True(this.signal.WaitOne(0));
        }
    }
}
