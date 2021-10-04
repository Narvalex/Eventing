using Infrastructure.EventSourcing;
using System.Collections.Generic;
using Xunit;

namespace Infrastructure.Tests.EventSourcing
{
    public class EntityFixture
    {
        [Fact]
        public void when_comparing_two_entities_with_same_id_then_they_are_equal()
        {
            var item1 = new ItemEntity(1, "name", 10);
            var item2 = new ItemEntity(1, "name", 10);
            var item3 = new ItemEntity(1, "other name", int.MaxValue);

            Assert.Equal(item1, item2);
            Assert.True(item1 == item2);

            Assert.Equal(item2, item3);
            Assert.True(item2 == item3);
        }

        [Fact]
        public void when_adding_to_a_hast_set_two_value_objects_that_are_equal_then_throws()
        {
            var set = new HashSet<ItemEntity>();
            set.Add(new ItemEntity(1, "name 1", 10));
            set.Add(new ItemEntity(1, "name other", 15450));
            set.Add(new ItemEntity(1, "name foo", 105468));

            Assert.Single(set);
            Assert.True(set.Count == 1);
        }
    }

    public class ItemEntity : DeprecatedEntity<int, ItemEntity>
    {
        public ItemEntity(int id, string name, int count) : base(id)
        {
            this.Name = name;
            this.Count = count;
        }

        public string Name { get; }
        public int Count { get; }
    }
}
