using Eventing.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eventing.OfflineClient.EntityFramework.Tests
{
    [TestClass]
    public class GivenNoDatabase : EntityFramworkPendingMessageQueueSpec
    {
        public GivenNoDatabase()
        {
            this.Cleanup();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.repository.DropDb();
            Assert.IsFalse(this.repository.DatabseExists);
        }

        [Test]
        public void CanDetectTheAbsenceOfADb()
        {
            var exists = this.repository.DatabseExists;

            Assert.IsFalse(exists);
        }

        [Test]
        public void CanCreateANewOne()
        {
            Assert.IsFalse(this.repository.DatabseExists);

            this.repository.CreateDbIfNotExists();

            Assert.IsTrue(this.repository.DatabseExists);
        }
    }
}
