using System;
using System.Collections.Generic;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class ForLoopTests
    {
        [Fact]
        public void can_add_item_to_list_in_for_loop()
        {
            var list = new List<string>
            {
                "hello", "world", "how", "are", "you"
            };

            Assert.Equal(5, list.Count);

            var computed = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Console.WriteLine(list[i]);

                if (i == 2)
                    list.Add("test");

                computed += 1;
            }

            Assert.Equal(6, list.Count);
            Assert.Equal(6, computed);
        }

        [Fact]
        public void can_not_add_item_to_list_in_forEach_loop()
        {
            var list = new List<string>
            {
                "hello", "world", "how", "are", "you"
            };

            Assert.Equal(5, list.Count);

            var computed = 0;

            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var item in list)
                {
                    Console.WriteLine(item);

                    if (computed == 2)
                        list.Add("test");

                    computed += 1;
                }
            });
        }
    }
}
