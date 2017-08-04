using Eventing.Core.Serialization;
using Eventing.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

namespace Eventing.OfflineClient.Tests
{
    [TestClass]
    public class GivenNoConnection : MessageOutboxSpec
    {
        private MessageOutbox sut;

        public GivenNoConnection()
        {
            this.sut = new MessageOutbox(this.HttpClient, new NewtonsoftJsonSerializer(TypeNameHandling.None),
                new InMemoryPendingMessagesQueue());
            this.HttpClient.SetOffline();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.sut.Dispose();
        }

        [Test]
        public void WhenSendingMessageThenReturnsEnqueued()
        {
            var result = this.sut.Send<LoginDto>("uri", new LoginDto { User = "testUser", Password = "1234" }).Result;
            Assert.AreEqual(OutboxSendStatus.Enqueued, result);
        }

        [Test]
        public void WhenSendingMessageExpectingResponseThenReturnsEnqueuedAndTheResponseIsNull()
        {
            var response = this.sut.Send<object, object>("uri", new LoginDto { User = "testUser", Password = "1234" }).Result;
            Assert.AreEqual(OutboxSendStatus.Enqueued, response.Status);
            Assert.IsNull(response.Result);
        }

        [Test]
        public void WhenSendingMessageThenRetriesContinously()
        {
            this.sut.Send<object>("uri", new LoginDto { User = "testUser", Password = "1234" }).Wait();
            Is.TrueThat(() => this.HttpClient.TryToSendCount > 1, TimeSpan.FromSeconds(3));
        }
    }
}
