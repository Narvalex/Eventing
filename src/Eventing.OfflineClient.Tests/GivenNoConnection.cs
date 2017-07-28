using Eventing.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eventing.OfflineClient.Tests
{
    [TestClass]
    public class GivenNoConnection : MessageOutboxSpec
    {
        private MessageOutbox sut;

        public GivenNoConnection()
        {
            //this.sut = new MessageOutbox();
        }

        [Test]
        public void WhenSendingMessageThenReturnsEnqueued()
        {
            //var result = this.sut.Send(new object()).Result;
            //Assert.AreEqual(OutboxSendStatus.Enqueued, result);
        }
    }
}
