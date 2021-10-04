using System;
using System.Diagnostics;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    // Thanks to https://elcamino.dev/diferencia-entre-float-double-y-decimal-en-c/?utm_source=youtube&utm_medium=video&utm_campaign=trafico_sitio
    public class DoubleAndDecimal
    {
        [Fact]
        public static void DoubleAddition()
        {
            Double x = .1;
            Double result = 10 * x;
            //After several operations, the double type increases its imprecision
            Double result2 = x + x + x + x + x + x + x + x + x + x;
            Assert.NotEqual(result, result2);
        }
        [Fact]
        public static void DecimalAddition()
        {
            Decimal x = .1m;
            Decimal result = 10 * x;
            //After several operations the decimal type remains accurate.
            Decimal result2 = x + x + x + x + x + x + x + x + x + x;
            Assert.Equal(result, result2);
        }

        //[Fact]
        internal static void DoubleTest()
        {
            Stopwatch watch = new Stopwatch();
            int iterations = 100000;

            //Double
            watch.Start();
            double z = 0;
            for (int i = 0; i < iterations; i++)
            {
                double x = i;
                double y = x * i;
                z += y;
            }
            watch.Stop();
            var doubleElapsedTime = watch.ElapsedTicks;
            //Decimal
            watch.Start();
            decimal zd = 0;
            for (int i = 0; i < iterations; i++)
            {
                decimal xd = i;
                decimal yd = xd * i;
                zd += yd;
            }
            watch.Stop();
            var decimalElapsedTime = watch.ElapsedTicks;
            //Double type is more efficient than decimal type
            Assert.True(decimalElapsedTime > doubleElapsedTime);
            //Processing calculations with the double type is at least 9 times faster than with the decimal type.
            Assert.True((decimalElapsedTime / doubleElapsedTime) > 9);
        }

        //Conclusion: the double data type should be used for performance purposes,
        //while the decimal data type should be used for precision.
    }
}
