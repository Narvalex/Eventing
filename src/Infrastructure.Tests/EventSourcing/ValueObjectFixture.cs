using Infrastructure.EventSourcing;
using System.Collections.Generic;
using Xunit;

namespace Infrastructure.Tests.EventSourcing
{
    public class ValueObjectFixture
    {
        [Fact]
        public void when_comparing_two_value_objects_with_same_value_then_they_are_equal()
        {
            var item1 = new Item("name", 10);
            var item2 = new Item("name", 10);

            Assert.Equal(item1, item2);
            Assert.True(item1 == item2);
        }

        [Fact]
        public void when_comparing_two_value_objects_with_just_one_different_value_then_they_are_different()
        {
            var item1 = new Item("name", 10);
            var item2 = new Item("name", 11);

            Assert.NotEqual(item1, item2);
            Assert.False(item1 == item2);

            var fields = item1.GetDiferentFieldValuesOfAnotherObject(item2);
            Assert.Single(fields);
        }

        [Fact]
        public void when_adding_to_a_hast_set_two_value_objects_that_are_equal_then_throws()
        {
            var set = new HashSet<Item>();
            set.Add(new Item("name", 10));
            set.Add(new Item("name", 10));
            set.Add(new Item("name", 10));

            Assert.Single(set);
        }
    }

    public class Item : ValueObject<Item>
    {
        private readonly string name;
        private readonly int count;

        public Item(string name, int count)
        {
            this.name = name;
            this.count = count;
        }

        public string Name => name;
        public int Count => count;
    }
}
