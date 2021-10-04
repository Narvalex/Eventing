using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Xunit;

namespace Infrastructure.Tests.EventSourcing
{
    public class EntitiesTest
    {
        private IJsonSerializer serializer = new NewtonsoftJsonSerializer();

        public EntitiesTest()
        {
            EventSourced.SetValidNamespace("Infrastructure.Tests.EventSourcing");
        }

        [Fact]
        public void can_create_deep_nested_sub_entities()
        {
            var cmd = new CrearGalaxiaViaLactea();
            ((ICommandInTransit)cmd).SetMetadata(new MessageMetadata("1", "TestRunner", "0.0.0.0", "test_browser"));

            var universo = EventSourcedCreator.New<Galaxia>();

            universo
                .Update(cmd, new GalaxiaCreada(cmd.Id))
                .Update(cmd, new PlanetaMarteRegistrado(cmd.Id))
                // Tierra events
                .Update(cmd, new NuevaEstacionTerrestreRegistrada(cmd.Id, "Primavera"))
                .Update(cmd, new NuevaEstacionTerrestreRegistrada(cmd.Id, "Verano"))
                .Update(cmd, new NuevoContinenteRegistrado(cmd.Id, 1, "América"))
                .Update(cmd, new NuevoContinenteRegistrado(cmd.Id, 2, "África"))
                .Update(cmd, new NuevoTipoDeSerVivoRegistrado(cmd.Id, 1, "Animal"))
                .Update(cmd, new NuevoTipoDeSerVivoRegistrado(cmd.Id, 2, "Planta"))
                // Contientes events
                .Update(cmd, new NuevoPaisRegistrado(cmd.Id, 1, 1, "Py"))
                .Update(cmd, new NuevoPaisRegistrado(cmd.Id, 1, 2, "Argentina"))
                .Update(cmd, new NuevoPaisRegistrado(cmd.Id, 2, 1, "Kenia"))
                .Update(cmd, new NuevoPaisRegistrado(cmd.Id, 2, 2, "Sudáfrica"))
                .Update(cmd, new NombreDePaisCorregido(cmd.Id, 1, 1, "Paraguay"))
            ;

            ((IEventSourced)universo).ExtractPendingEvents();

            var serialized = this.serializer.Serialize(universo);

            universo = this.serializer.Deserialize<Galaxia>(serialized);
            var serialized2 = this.serializer.Serialize(universo);

            Assert.Equal(serialized, serialized2);
        }
    }
}
