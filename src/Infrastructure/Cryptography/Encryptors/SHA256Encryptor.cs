using System.Security.Cryptography;

namespace Infrastructure.Cryptography
{
    public class SHA256Encryptor : HashEncryptor
    {
        public SHA256Encryptor() : base(Create) {}
        private static HashAlgorithm Create() => new SHA256Managed();
    }
}
