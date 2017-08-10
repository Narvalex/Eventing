using Eventing.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Eventing.OfflineClient.EntityFramework.Tests
{
    [TestClass]
    public class GivenEmptyDatabaseAndNoConnection : EntityFramworkPendingMessageQueueSpec
    {
        public GivenEmptyDatabaseAndNoConnection()
        {
            this.repository.CreateDbIfNotExists();
            this.http.SetOffline();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.repository.DropDb();
            Assert.IsFalse(this.repository.DatabseExists);
        }

        [Test]
        public void WhenFirstMessageFailsThenIsEnqueued()
        {
            var url = "www.google.com/api";
            var cmd = new Command1(2, "anonymous");
            this.outbox.Send(url, cmd).Wait();

            PendingMessage msg = null;
            this.repository.TryPeek(out msg);

            Assert.AreEqual(url, msg.Url);

            using (var ctx = this.contextFactory.Invoke(true))
            {
                Is.TrueThat(() => ctx.PendingMessageQueue.Count() == 1, TimeSpan.FromSeconds(10));
                var entity = ctx.PendingMessageQueue.FirstOrDefault();
                Assert.IsNotNull(entity);
            }
        }

        public class Command1
        {
            public Command1(int id, string name)
            {
                this.Id = id;
                this.Name = name;
            }

            public int Id { get; }
            public string Name { get; }
        }
    }
}
