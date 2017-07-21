using Eventing.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Inventory.Domain.Tests
{
    [TestClass]
    public class GivenExistingItem : InventoryItemSpec
    {
        private Guid id = Guid.NewGuid();

        public GivenExistingItem()
        {
            this.sut.Given<InventoryItem>(this.id.ToString(),
                new InventoryItemCreated(this.id, "Item1"));
        }

        [Test]
        public void ThenCanCheckInItems()
        {
            this.sut.When(s =>
            {
                s.HandleAsync(new CheckInItemsToInventory(this.id, 2)).Wait();
            })
            .Then(evs =>
            {
                Assert.AreEqual(1, evs.Count);
                var e = evs.OfType<ItemsCheckedInToInventory>().Single();
                Assert.AreEqual(2, e.Count);
                Assert.AreEqual(this.id, e.Id);
            });
        }

        [Test]
        public void WhenCheckInZeroThenThrows()
        {
            this.sut.When(s =>
            {
                Assert.ThrowsException<InvalidCommandException>(() =>
                {
                    try
                    {
                        s.HandleAsync(new CheckInItemsToInventory(this.id, 0)).Wait();
                    }
                    catch (Exception ex)
                    {
                        throw ex.InnerException;
                    }
                });
            });
        }

        [Test]
        public void WhenCheckInMinusOneThenThrows()
        {
            this.sut.When(s =>
            {
                Assert.ThrowsException<InvalidCommandException>(() =>
                {
                    try
                    {
                        s.HandleAsync(new CheckInItemsToInventory(this.id, -1)).Wait();
                    }
                    catch (Exception ex)
                    {
                        throw ex.InnerException;
                    }
                });
            });
        }
    }
}
