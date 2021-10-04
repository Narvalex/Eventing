using Infrastructure.EventSourcing.Transactions;
using Infrastructure.Messaging;
using System;
using System.Collections.Generic;

namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// Something that is event sourced. 
    /// If it contains a lot of sub items like <see cref="SubEntities{TEntity}"/> 
    /// and/or <see cref="EventSourcedSection"/> and maybe <see cref="ValueObject{T}"/> 
    /// then is an "Aggregate". Otherwise if it is rather simple, then it is an 
    /// entity. If its main porpouse is to verify uniqueness of items in a set 
    /// then it is an event sourced "Index". 
    /// Also could be a <see cref="ISagaExecutionCoordinator"/>.
    /// </summary>
    public interface IEventSourced
    {
        string Id { get; }
        EventSourcedMetadata Metadata { get; }
        void Apply(IEvent @event);
        void RegisterOutputSectionStateAction(Action action);
        void ApplyOutputState();
        void Update(string correlationId, string causationId, long? causationNumber, IMessageMetadata metadata, bool isCommandMetadata, IEventInTransit @event);
        IEnumerable<IEventInTransit> ExtractPendingEvents();
        int GetPendingEventsCount();
        string GetEntityType();
        string GetAssembly();
        IEnumerable<Type> GetSourcingEventTypes();
        UpdateEventSourcedParams? GetPrepareEventParams();
    }
}
