using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class TernaryExpression
    {
        [Fact]
        public void false_clause_not_executed_Test()
        {
            var hello = false ? ExceptionThrower() : "hello";
        }

        private string ExceptionThrower()
        {
            Assert.True(false);
            return "hi";
        }
    }
}
