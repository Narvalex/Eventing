using Infrastructure.IdGeneration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.IdGeneration
{
    // Tests based on: https://github.com/MicrosoftArchive/cqrs-journey/blob/master/source/Conference/Registration.Tests/HandleGeneratorFixture.cs
    public abstract class IdGeneratorSpecification
    {
        private readonly IUniqueIdGenerator sut;
        private readonly Func<int, IUniqueIdGenerator> customLengthFactory;

        public IdGeneratorSpecification(IUniqueIdGenerator sut, Func<int, IUniqueIdGenerator> customLengthFactory = null)
        {
            this.sut = sut;
            this.customLengthFactory = customLengthFactory;
        }

        [Theory]
        [InlineData(5)]
        [InlineData(20)]
        public void WhenGeneratingIdThenGeneratesConfiguredLength(int length)
        {
            if (this.customLengthFactory is null)
                return; 

            var id = this.customLengthFactory(length).New();

            Assert.Equal(length, id.Length);
        }

        [Fact]
        public void WhenGeneratingIdsThenGeneratesDifferentValues()
        {
            Assert.NotEqual(this.sut.New(), this.sut.New());
        }

        [Fact]
        public void GenerationIsThreadSafe()
        {
            var list = new ConcurrentBag<string>();
            Parallel.For(0, 100, i => list.Add(this.sut.New()));

            Assert.Equal(100, list.Count);
        }

        [Theory]
        [InlineData(100)]
        //[InlineData(50_000_000)]
        public void ShouldGenerateDistinctIds(int totalUniqueIds)
        {
            var list = new ConcurrentBag<string>();
            Parallel.For(0, totalUniqueIds, i => list.Add(this.sut.New()));

            Assert.Equal(totalUniqueIds, list.Distinct().Count());
        }
    }

    public class GivenRandomAllCasesGenerator : IdGeneratorSpecification
    {
        public GivenRandomAllCasesGenerator() 
            : base(new AllCaseUniqueIdGenerator(), length => new AllCaseUniqueIdGenerator(length))
        { }
    }

    public class GivenRandomSingleCaseGenerator : IdGeneratorSpecification
    {
        public GivenRandomSingleCaseGenerator()
            : base(new SingleCaseUniqueIdGenerator(), length => new SingleCaseUniqueIdGenerator(false, false, length))
        { }
    }

    public class GivenRandomNumbersGenerator : IdGeneratorSpecification
    {
        public GivenRandomNumbersGenerator() 
            : base(new NumberedUniqueIdGenerator(), length => new NumberedUniqueIdGenerator(length))
        { }
    }

    public class GivenKestrelUniqueIdGenerator : IdGeneratorSpecification
    {
        public GivenKestrelUniqueIdGenerator()
            : base(new KestrelUniqueIdGenerator())
        { }
    }
}
