using System.Security.Cryptography;

namespace Infrastructure.Cryptography
{
    public class MD5Encryptor : HashEncryptor
    {
        public MD5Encryptor() : base(Create) { }
        private static HashAlgorithm Create() => new MD5CryptoServiceProvider();
    }
}
