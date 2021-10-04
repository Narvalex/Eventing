using Infrastructure.Serialization;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class DictionarySerialization
    {
        private IJsonSerializer sut = new NewtonsoftJsonSerializer();

        [Fact]
        public void cand_serialize_dictionary()
        {
            var dictionary = new Dictionary<string, string[]>();
            dictionary.Add("acme", new string[] { "claim1", "claim2", "claim3" });
            dictionary.Add("contoso", new string[] { "claim4", "claim5" });

            var serialized = this.sut.Serialize(dictionary);

            // serialization output is ugly
        }

    }
}
