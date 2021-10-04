using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Erp.Domain.Tests.Helpers
{
    public class TestableEventHandlerES
    {
        private readonly TestableEventHandler testableHandler;

        public TestableEventHandlerES(Func<IEventSourcedRepository, IEventHandler> handlerFactory, string namespaceValido, string assembly)
        {
            this.testableHandler = new TestableEventHandler(handlerFactory, namespaceValido, assembly);
        }
        /// <summary>
        /// Eventos que modifican, hidratan al aggregate
        /// </summary>
        /// <typeparam name="TEventSourced"></typeparam>
        /// <param name="historia"></param>
        /// <returns></returns>
        public TestableEventHandlerES Dado<TEventSourced>(params IEventInTransit[] historia) where TEventSourced : class, IEventSourced
        {
            this.testableHandler.Given<TEventSourced>(historia);
            return this;
        }

        public TestableEventHandlerES Dada<TEventSourced>(params IEventInTransit[] historia) where TEventSourced : class, IEventSourced
        {
            this.testableHandler.Given<TEventSourced>(historia);
            return this;
        }

        /// <summary>
        /// (Evento, comando) Externo, es un mensaje entrante
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        public TestableEventHandlerES Cuando(IEventInTransit evento)
        {
            this.testableHandler.When(evento);
            return this;
        }

        /// <summary>
        /// Emite eventos
        /// </summary>
        /// <typeparam name="TEvent">Evento que el aggregate emite</typeparam>
        /// <param name="predicado">Es un delegado que representa el evento contra el cual hacer las asserciones</param>
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

        public Task Entonces<TEvent>(string streamIdEsperado, Func<TEvent, string> propiedadDelEventoQueIndicaElStreamId, string correlationIdEsperado, Action<TEvent> predicado = null)
        {
            return this.testableHandler.Then(streamIdEsperado, propiedadDelEventoQueIndicaElStreamId, correlationIdEsperado, predicado);
        }

        public Task Entonces<TEvent>(string streamIdEsperado, string correlationIdEsperado, Action<TEvent> predicado = null) where TEvent : IEvent
        {
            return this.testableHandler.Then(streamIdEsperado, correlationIdEsperado, predicado);
        }

        public Task EntoncesVarios<TEvent>(Action<IList<TEvent>> predicado = null)
        {
            return this.testableHandler.ThenSome(predicado);
        }

        /// <summary>
        /// El Aggregate atraves del eventHandler emite unicamente este evento como consecuencia del manejo del evento entrante
        /// especificado en el cuando
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="predicado"></param>
        /// <returns></returns>
        public Task EntoncesUnicamente<TEvent>(Action<TEvent> predicado = null)
        {
            return this.testableHandler.ThenOnly(predicado);
        }

        public Task EntoncesUnicamente<TEvent>(string streamIdEsperado, Action<TEvent> predicado = null) where TEvent : IEvent
        {
            return this.testableHandler.ThenOnly(streamIdEsperado, predicado);
        }

        public Task EntoncesUnicamente<TEvent>(string streamIdEsperado, Func<TEvent, string> propiedadDelEventoQueIndicaElStreamId, Action<TEvent> predicado = null)
        {
            return this.testableHandler.ThenOnly(streamIdEsperado, propiedadDelEventoQueIndicaElStreamId, predicado);
        }

        public Task EntoncesUnicamente<TEvent>(string streamIdEsperado, Func<TEvent, string> propiedadDelEventoQueIndicaElStreamId, string correlationIdEsperado, Action<TEvent> predicado = null)
        {
            return this.testableHandler.ThenOnly(streamIdEsperado, propiedadDelEventoQueIndicaElStreamId, correlationIdEsperado, predicado);
        }

        public Task EntoncesUnicamente<TEvent>(string streamIdEsperado, string correlationIdEsperado, Action<TEvent> predicado = null) where TEvent : IEvent
        {
            return this.testableHandler.ThenOnly(streamIdEsperado, correlationIdEsperado, predicado);
        }


        /// <summary>
        /// Entonces el aggregate no emitio ni un solo evento al procesar un mensaje entrante.
        /// Generalmente hacemos esto para ser idempotentes o el handler no esta interesado.
        /// </summary>
        public Task EntoncesNada()
        {
            return this.testableHandler.ThenNoOp();
        }
    }
}
