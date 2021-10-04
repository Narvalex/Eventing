using Infrastructure.Serialization;
using System.Collections.Generic;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class WeirdSerializationBehavior
    {
        private IJsonSerializer sut = new NewtonsoftJsonSerializer();

        [Fact]
        public void serializer_can_deserialize_collections_even_if_constructor_argument_names_are_misspelled()
        {
            var dto = new PersonDto("Juan", new List<string> { "Copy", "Paste" }, new HashSet<string> { "Happy people" });
            var serialized = this.sut.Serialize(dto);

            var dto2 = this.sut.Deserialize<PersonDto>(serialized);

            Assert.Equal(dto.Name, dto2.Name);
            Assert.Equal(dto.Grants, dto2.Grants);
            Assert.Equal(dto.Groups, dto2.Groups);
        }
    }

    public class PersonDto
    {
        public PersonDto(string name, List<string> hahaha, HashSet<string> hohohoho)
        {
            this.Name = name;
            this.Grants = hahaha ?? new List<string>();
            this.Groups = hohohoho ?? new HashSet<string>();
        }

        public string Name { get; }
        public List<string> Grants { get; }
        public HashSet<string> Groups { get; }
    }
}
