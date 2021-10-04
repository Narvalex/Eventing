using System.Security.Cryptography;

namespace Infrastructure.Cryptography
{
    public class SHA512Encryptor : HashEncryptor
    {
        public SHA512Encryptor() : base(Create) { }
        private static HashAlgorithm Create() => new SHA512Managed();
    }
}
