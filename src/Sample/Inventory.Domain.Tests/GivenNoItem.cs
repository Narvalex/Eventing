using Eventing.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Inventory.Domain.Tests
{
    [TestClass]
    public class GivenNoItem : InventoryItemSpec
    {
        [Test]
        public void ThenCanCreateANewItem()
        {
            var id = Guid.NewGuid();
            var name = "Item1";
            this.sut.When(s =>
            {
                s.HandleAsync(new CreateInventoryItem(id, name)).Wait();
            })
            .Then(evs =>
            {
                Assert.AreEqual(1, evs.Count);
                var e = evs.Single();
                Assert.AreEqual(typeof(InventoryItemCreated), e.GetType());
            });
        }
    }
}
