using System;

namespace Infrastructure.IdGeneration
{
    public class AllCaseUniqueIdGenerator : IUniqueIdGenerator
    {
        // Do not forget the birthay paradox
        // url: https://betterexplained.com/articles/understanding-the-birthday-paradox/

        private static readonly Random random = new Random(DateTime.UtcNow.Millisecond);

        private static readonly char[] allowableChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        private readonly int length;

        // youtube uses 11.
        public AllCaseUniqueIdGenerator(int length = 11)
        {
            this.length = length;
        }

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
