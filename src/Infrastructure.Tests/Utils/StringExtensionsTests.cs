using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Utils
{
    public class StringExtensionsTests
    {
        [Fact]
        public void accents_are_replaced()
        {
            var text = "José Giménez, útil, mamá, á, é, í, ó, ú";
            var textExpected = "Jose Gimenez, util, mama, a, e, i, o, u";

            var textResult = text.WithDiacriticsRemoved();

            Assert.Equal(textExpected, textResult);
        }
    }
}
