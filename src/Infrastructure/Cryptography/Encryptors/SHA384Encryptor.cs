using System.Security.Cryptography;

namespace Infrastructure.Cryptography
{
    public class SHA384Encryptor : HashEncryptor
    {
        public SHA384Encryptor() : base(Create) { }
        private static HashAlgorithm Create() => new SHA384Managed();
    }
}
