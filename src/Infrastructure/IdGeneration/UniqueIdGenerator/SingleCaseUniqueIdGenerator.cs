using System;

namespace Infrastructure.IdGeneration
{
    public class SingleCaseUniqueIdGenerator : IUniqueIdGenerator
    {
        // Do not forget the birthay paradox
        // url: https://betterexplained.com/articles/understanding-the-birthday-paradox/

        private static readonly Random random = new Random(DateTime.UtcNow.Millisecond);

        private static char[] allowableChars;
        private readonly int lenght;

        /// <summary>
        /// Creates a the random id generator. 
        /// </summary>
        /// <param name="upperCase">
        /// If set to true, the case will be uuper. Otherwise, it will be lower case.
        /// </param>
        /// <param name="lenght">
        /// The default length for 10.000 uniques combinations is 6. 
        /// large blogs (like codementor) uses 9
        /// Amazon uses 10 characters, like "B07FDKZQTY".
        /// Team Id of Apple uses 10 characters.
        /// W3Schools usa para sus fileName (archivo generado por los usuarios) 12 caracteres, como 'FULI893TMO2S'
        /// Microsoft uses 16 for its accounts, for onedrive. Looks like this is good, like guids!
        /// </param>
        public SingleCaseUniqueIdGenerator(bool upperCase = false, bool onlyLetters = false, int lenght = 12)
        {
            this.lenght = lenght;
            var upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            allowableChars = upperCase ? upperChars.ToCharArray() : upperChars.ToLowerInvariant().ToCharArray();
            if (!onlyLetters)
                upperChars += "0123456789";
        }

        public string New() => this.New(this.lenght);

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
