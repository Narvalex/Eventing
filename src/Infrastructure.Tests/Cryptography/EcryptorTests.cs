using Infrastructure.Cryptography;
using Xunit;

namespace Infrastructure.Tests.Cryptography
{
    public abstract class given_encryptor_specification
    {
        protected IEncryptor sut;
        public given_encryptor_specification(IEncryptor encryptor)
        {
            this.sut = encryptor;
        }

        [Fact]
        public void when_encrypting_null_then_returns_null()
        {
            var encrypted = this.sut.Encrypt(null);

            Assert.Null(encrypted);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Password123")]
        public void when_encrypting_text_then_encrypted_is_diferent_from_original_value(string value)
        {
            var encrypted = this.sut.Encrypt(value);

            Assert.NotEqual(value, encrypted);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Password123")]
        public void when_encrypting_text_twice_then_encrypted_value_is_the_same(string value)
        {
            var encrypted1 = this.sut.Encrypt(value);
            var encrypted2 = this.sut.Encrypt(value);

            Assert.Equal(encrypted2, encrypted1);
        }
    }
    public class given_md5 : given_encryptor_specification
    {
        public given_md5() : base(new MD5Encryptor()) { }
    }
    public class given_sha1 : given_encryptor_specification
    {
        public given_sha1() : base(new SHA1Encryptor()) { }
    }
    public class given_sha256 : given_encryptor_specification
    {
        public given_sha256() : base(new SHA256Encryptor()) { }
    }
    public class given_sha384 : given_encryptor_specification
    {
        public given_sha384() : base(new SHA384Encryptor()) { }
    }
    public class given_sha512 : given_encryptor_specification
    {
        public given_sha512() : base(new SHA512Encryptor()) { }
    }
}
