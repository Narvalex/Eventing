namespace Infrastructure.Cryptography
{
    public interface IEncryptor
    {
        string Encrypt(string text);
    }
}
