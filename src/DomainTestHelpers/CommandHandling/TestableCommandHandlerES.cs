using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Erp.Domain.Tests.Helpers
{
    /// <summary>
    /// La versión en español de <see cref="TestableHandler{T}"/>. 
    /// Es útil que esté en español para que el desarrollo de los 
    /// tests tenga más sentido.
    /// </summary>
    public class TestableCommandHandlerES
    {
        private readonly TestableCommandHandler testableHandler;

        public TestableCommandHandlerES(Func<IEventSourcedRepository, ICommandHandler> handlerFactory, string namespaceValido, string assembly)
        {
            this.testableHandler = new TestableCommandHandler(handlerFactory, namespaceValido, assembly);
        }

        public void SetOnVerificarSerializacionDelComando(Func<ICommandInTest, ICommandInTest> transformacion)
        {
            this.testableHandler.SetOnEnsureCommandSerializationIsValid(transformacion);
        }

        public TestableCommandHandlerES Dado<TEventSourced>(params IEventInTransit[] historia) where TEventSourced : class, IEventSourced
        {
            this.testableHandler.Given<TEventSourced>(historia);
            return this;
        }

        public TestableCommandHandlerES Dada<TEventSourced>(params IEventInTransit[] historia) where TEventSourced : class, IEventSourced
        {
            this.testableHandler.Given<TEventSourced>(historia);
            return this;
        }

        public TestableCommandHandlerES Cuando(ICommandInTest comando, MessageMetadata metadatos = null)
        {
            this.testableHandler.When(comando, metadatos);
            return this;
        }

        public Task Entonces<TEvent>(Action<TEvent> predicado = null)
        {
            return this.testableHandler.Then(predicado);
        }

        public Task Entonces<TEvent>(string streamIdEsperado, Action<TEvent> predicado = null) where TEvent : IEvent
        {
            return this.testableHandler.Then(streamIdEsperado, predicado);
        }

        public Task Entonces<TEvent>(string streamIdEsperado, Func<TEvent, string> propiedadDelEventoQueIndicaElStreamId, Action<TEvent> predicado = null)
        {
            return this.testableHandler.Then(streamIdEsperado, propiedadDelEventoQueIndicaElStreamId, predicado);
        }

        public Task EntoncesTodos<TEvent>(Action<IList<TEvent>> predicado = null)
        {
            return this.testableHandler.ThenAll(predicado);
        }

        public Task EntoncesUnicamente<TEvent>(Action<TEvent> predicado = null)
        {
            return this.testableHandler.ThenOnly(predicado);
        }

        public Task EntoncesUnicamente<TEvent>(string streamIdEsperado, Action<TEvent> predicado = null) where TEvent : IEvent
        {
            return this.testableHandler.ThenOnly<TEvent>(streamIdEsperado, predicado);
        }

        public Task EntoncesUnicamente<TEvent>(string streamIdEsperado, Func<TEvent, string> propiedadDelEventoQueIndicaElStreamId, Action<TEvent> predicado = null)
        {
            return this.testableHandler.ThenOnly(streamIdEsperado, propiedadDelEventoQueIndicaElStreamId, predicado);
        }

        public Task EntoncesNada()
        {
            return this.testableHandler.ThenNoOp();
        }

        public Task EntoncesElComandoEsAceptado()
        {
            return this.testableHandler.ThenCommandIsAccepted();
        }

        public Task EntoncesElComandoEsAceptado<T>(Action<T> predicado = null)
        {
            return this.testableHandler.ThenCommandIsAccepted<T>(predicado);
        }

        public Task EntoncesElComandoEsRechazado()
        {
            return this.testableHandler.ThenCommandIsRejected();
        }

        public Task EntoncesArrojaUnForeignKeyViolationException()
        {
            return this.testableHandler.ThenThrowsForeignKeyViolationException();
        }
    }
}
