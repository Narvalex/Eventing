using Infrastructure.EventSourcing;
using Xunit;

namespace Infrastructure.Tests.EventSourcing
{
	public class EventSourcedEntityHasherTests
	{
		public EventSourcedEntityHasherTests()
		{
			EventSourced.SetValidNamespace("Infrastructure.Tests.EventSourcing");
		}

		[Fact]
		public void should_always_emit_the_same_hash_for_an_empty_entity()
		{
			this.TestHashFor<StatelessTestEntity>(
				hash: "25c8405ead481a30fbb7cc33051ca5a41fb543f50038ab98cb3ad2970962a840",
				constructorIL: @"
	IL_0000: ldarg.0
	IL_0001: ldarg.1
	IL_0002: call void Infrastructure.EventSourcing.EventSourced::.ctor(Infrastructure.EventSourcing.EventSourcedMetadata)
	IL_0007: nop
	IL_0008: nop
	IL_0009: ret
",
				handlersIL: @"
	IL_0000: nop
	IL_0001: ret
",
				outputStateIL: @"
	IL_0000: nop
	IL_0001: ret

"
			);
		}

		[Fact]
		public void should_always_emit_the_same_hash_for_a_very_simple_entity()
		{
			this.TestHashFor<EntityA>(
				hash: "93284a67b57ad8a318be01d43adad9c6037478373dbd887934526682c9e83b27",
				constructorIL: @"
	IL_0000: ldarg.0
	IL_0001: ldarg.1
	IL_0002: call void Infrastructure.EventSourcing.EventSourced::.ctor(Infrastructure.EventSourcing.EventSourcedMetadata)
	IL_0007: nop
	IL_0008: nop
	IL_0009: ldarg.0
	IL_000a: ldarg.2
	IL_000b: call void Infrastructure.Tests.EventSourcing.EntityA::set_Name(string)
	IL_0010: nop
	IL_0011: ret

",
				handlersIL: @"
	IL_0000: nop
	IL_0001: ret
",
				outputStateIL: @"
	IL_0000: nop
	IL_0001: ret
"
			);
		}

		[Fact]
		public void should_always_emit_the_same_hash_for_a_entity()
		{
			this.TestHashFor<TestEntities>(
				hash: "2e3b64766c74061151092159755ec0afacbde407b07cfa5fca41e5f50c5841d6",
				constructorIL: @"
	IL_0000: ldarg.0
	IL_0001: ldc.i4.0
	IL_0002: stfld bool Infrastructure.Tests.EventSourcing.TestEntities::<Deleted>k__BackingField
	IL_0007: ldarg.0
	IL_0008: ldarg.1
	IL_0009: call void Infrastructure.EventSourcing.EventSourced::.ctor(Infrastructure.EventSourcing.EventSourcedMetadata)
	IL_000e: nop
	IL_000f: nop
	IL_0010: ldarg.0
	IL_0011: ldarg.2
	IL_0012: call void Infrastructure.Tests.EventSourcing.TestEntities::set_Deleted(bool)
	IL_0017: nop
	IL_0018: ret

",
				handlersIL: @"
	IL_0000: nop
	IL_0001: ldarg.1
	IL_0002: callvirt Infrastructure.EventSourcing.IHandlerRegistry Infrastructure.EventSourcing.IHandlerRegistry::On()
	IL_0007: ldarg.0
	IL_0008: ldftn void Infrastructure.Tests.EventSourcing.TestEntities::<OnRegisteringHandlers>b__6_0(Infrastructure.Tests.EventSourcing.TestEventWithMetadata)
	IL_000e: newobj void System.Action`1[[Infrastructure.Tests.EventSourcing.TestEventWithMetadata, Infrastructure.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]::.ctor(System.Object, System.IntPtr)
	IL_0013: callvirt Infrastructure.EventSourcing.IHandlerRegistry Infrastructure.EventSourcing.IHandlerRegistry::On(System.Action`1[[Infrastructure.Tests.EventSourcing.TestEventWithMetadata, Infrastructure.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]])
	IL_0018: pop
	IL_0019: ret
",
				outputStateIL: @"
	IL_0000: nop
	IL_0001: ldarg.0
	IL_0002: ldarg.0
	IL_0003: call bool Infrastructure.Tests.EventSourcing.TestEntities::get_Deleted()
	IL_0008: call void Infrastructure.Tests.EventSourcing.TestEntities::set_Deleted(bool)
	IL_000d: nop
	IL_000e: ret
"
			);
		}

		private void TestHashFor<T>(string hash, string constructorIL, string handlersIL, string outputStateIL) where T : IEventSourced
		{
			var currentHash = EventSourcedEntityHasher.GetHash<T>();
			Assert.Equal(hash, currentHash);

			var actualCtorIL = EventSourcedEntityHasher.GetConstructorIL<T>();
			Assert.Equal(this.Normalize(constructorIL), this.Normalize(actualCtorIL));

			var actualHandlersIL = EventSourcedEntityHasher.GetHandlersIL<T>();
			Assert.Equal(this.Normalize(handlersIL), this.Normalize(actualHandlersIL));

			var actualOutputStateIL = EventSourcedEntityHasher.GetOnOutputStateIL<T>();
			Assert.Equal(this.Normalize(outputStateIL), this.Normalize(actualOutputStateIL));
		}

		private string Normalize(string str)
		{
			return str.Trim().Replace("\r\n", "\n");
		}
	}
}
