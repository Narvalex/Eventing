using Infrastructure.Utils;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Cryptography
{
    // Thanks to: http://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
    //
    public class StringCipher : IDecryptor
    {
        private const int KeySize = 128;
        private const int DerivationIterations = 2;

        private readonly string password;

        public StringCipher(string password = "trusted")
        {
            this.password = password ?? "trusted";
        }

        public string Encrypt(string text)
        {
            var saltBytes = this.Generate128BitsOfRandomEntropy();
            var ivBytes = this.Generate128BitsOfRandomEntropy();
            var textBytes = Encoding.UTF8.GetBytes(text);
            using (var password = new Rfc2898DeriveBytes(this.password, saltBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(KeySize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 128; //256 is not supported in .net core
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;

                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(textBytes, 0, textBytes.Length);
                            cryptoStream.FlushFinalBlock();

                            var cipherTextBytes = saltBytes;
                            cipherTextBytes = cipherTextBytes.Concat(ivBytes).ToArray();
                            cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                            return cipherTextBytes.ToHexString();
                        }
                    }
                }
            }

        }

        public string Decrypt(string text)
        {
            var completeCipherBytes = text.ToByteArrayFromHexString();
            var saltBytes = completeCipherBytes.Take(KeySize / 8).ToArray();
            var ivBytes = completeCipherBytes.Skip(KeySize / 8).Take(KeySize / 8).ToArray();
            var cipherBytes = completeCipherBytes.Skip((KeySize / 8) * 2).Take(completeCipherBytes.Length - ((KeySize / 8) * 2)).ToArray();

            using (var password = new Rfc2898DeriveBytes(this.password, saltBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(KeySize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 128;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;

                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivBytes))
                    using (var memoryStream = new MemoryStream(cipherBytes))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        var textBytes = new byte[cipherBytes.Length];
                        var decryptedByteCount = cryptoStream.Read(textBytes, 0, textBytes.Length);
                        return Encoding.UTF8.GetString(textBytes, 0, decryptedByteCount);
                    }
                }
            }
        }

        private byte[] Generate128BitsOfRandomEntropy()
        {
            var randomBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(randomBytes);
            return randomBytes;
        }
    }
}
