using Infrastructure.Utils;
using System;

namespace Infrastructure.IdGeneration
{
    public class NumberedUniqueIdGenerator : IUniqueIdGenerator
    {
        // Do not forget the birthay paradox
        // url: https://betterexplained.com/articles/understanding-the-birthday-paradox/

        private static readonly Random random = new Random(DateTime.UtcNow.Millisecond);

        private static readonly char[] allowableChars = "0123456789".ToCharArray();
        private readonly int length;

        /// <summary>
        /// Creates a new instance of <see cref="NumberedUniqueIdGenerator"/>.
        /// </summary>
        /// <param name="length">Based on Amazon order numbers: Like 112-4763078-0629069</param>
        public NumberedUniqueIdGenerator(int length = 17)
        {
            Ensure.Positive(length, nameof(length));

            this.length = length;
        }

        // Amazon like formating
        //public string New() => $"{this.New(3)}-{this.New(7)}-{this.New(7)}";

        public string New() => this.New(this.length);

        private string New(int length)
        {
            var result = new char[length];
            lock (random)
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = allowableChars[random.Next(0, allowableChars.Length)];
                }
            }

            return new string(result);
        }
    }
}
