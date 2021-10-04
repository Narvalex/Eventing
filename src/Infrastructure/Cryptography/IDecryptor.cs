namespace Infrastructure.Cryptography
{

    public interface IDecryptor : IEncryptor
    {
        string Decrypt(string text);
    }
}
