using Microsoft.Extensions.Caching.Memory;
using System;
using Xunit;

namespace Infrastructure.Tests.Snapshotting
{
    public class MemoryCacheSpecs
    {
        private MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        [Fact]
        public void ThrowsExpectdVersionOnDiposedCall()
        {
            this.memoryCache.Set("1", "Hello world");

            Assert.Equal("Hello world", memoryCache.Get("1"));

            this.memoryCache.Dispose();

            try
            {
                Assert.Equal("Hello world", memoryCache.Get("1"));
            }
            catch (ObjectDisposedException)
            {
                // Warming;
                this.memoryCache = new MemoryCache(new MemoryCacheOptions());
                this.memoryCache.Set("1", "Hello world");
                Assert.Equal("Hello world", memoryCache.Get("1"));
            }

        }
    }
}
