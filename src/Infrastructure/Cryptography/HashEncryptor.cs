using Infrastructure.Utils;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Cryptography
{
    public abstract class HashEncryptor : IEncryptor
    {
        private readonly Func<HashAlgorithm> algorithmFactory;      

        public HashEncryptor(Func<HashAlgorithm> algorithmFactory)
        {
            this.algorithmFactory = Ensured.NotNull(algorithmFactory, nameof(algorithmFactory));
        }      

        public string Encrypt(string text)
        {
            if (text == null) return null;

            using (var algorithm = this.algorithmFactory())
            {
                var byteResult = algorithm.ComputeHash(Encoding.UTF8.GetBytes(text));

                return string.Join(string.Empty, byteResult.Select(x => x.ToString("x2")));
            }
        }
    }
}
