using System.Security.Cryptography;

namespace Infrastructure.Cryptography
{
    public class SHA1Encryptor : HashEncryptor
    {
        public SHA1Encryptor() : base(Create) {}
        private static HashAlgorithm Create() => new SHA1Managed();
    }
}
