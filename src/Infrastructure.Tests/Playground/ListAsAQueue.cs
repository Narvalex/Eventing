using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class ListAsAQueue
    {
        [Fact]
        public void list_can_be_a_funny_queue()
        {
            var list = new List<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.Equal(1, list.First());

            // Dequeue
            list.Remove(list.First());

            Assert.Equal(2, list.First());
        }

        [Fact]
        public void hashset_can_be_a_funny_queue()
        {
            var list = new HashSet<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.Equal(1, list.First());

            // Dequeue
            list.Remove(list.First());

            Assert.Equal(2, list.First());
        }
    }
}
