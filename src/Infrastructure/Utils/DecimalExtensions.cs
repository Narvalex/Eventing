using System;
using System.Collections;

namespace Infrastructure.Utils
{
    public static class DecimalExtensions
    {
        // source: https://stackoverflow.com/questions/429165/raising-a-decimal-to-a-power-of-decimal
        public static decimal Pow(this decimal x, uint y)
        {
            var a = 1m;
            var e = new BitArray(BitConverter.GetBytes(y));
            var t = e.Count;

            for (int i = t - 1; i >= 0; --i)
            {
                a *= a;
                if (e[i] == true)
                    a *= x;
            }

            return a;
        }

        public static decimal Pow(this decimal x, int y)
        {
            if (y < 0) throw new ArgumentOutOfRangeException("The y value can not be lower than 0");
            return Pow(x, Convert.ToUInt32(y));
        }
    }
}
