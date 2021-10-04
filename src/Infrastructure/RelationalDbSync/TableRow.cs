using Infrastructure.Cryptography;
using Infrastructure.EventSourcing;
using Infrastructure.Utils;
using System;
using System.Linq;
using System.Text;

namespace Infrastructure.RelationalDbSync
{
    public abstract class TableRow<T> : ValueObject<T>, ITableRow
        where T : ValueObject<T>, ITableRow
    {
        static IEncryptor encriptor = new SHA512Encryptor();

        protected TableRow(params object?[] values)
        {
            if (!values.Any())
                throw new ArgumentOutOfRangeException("Al least one parameter is needed to make a key for the row.");

            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                var item = values[i];
                sb.Append($"$Param{i}");
                if (item is DateTime)
                    sb.Append(((DateTime)item).ToUniversalTime().ToString());
                else
                    sb.Append(item is null ? "null" : item!.ToString()!.WithWhiteSpacesReplaced("~"));
            }

            var hash = encriptor.Encrypt(sb.ToString());

            this.Key = $"{hash.Substring(0, 6)}{hash.Substring(hash.Length - 6, 6)}";
        }

        public string Key { get; }
    }
}
