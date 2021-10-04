using Infrastructure.EventSourcing;
using System;
using System.Collections.Generic;

namespace Infrastructure.RelationalDbSync
{
    public abstract class EventSourcedRow<TTableRow, TInsertedEvent, TDeletedEvent, TRestoredEvent, TUpdatedEvent> 
        : EventSourced, IEventSourcedRow<TTableRow, TInsertedEvent, TDeletedEvent, TRestoredEvent, TUpdatedEvent>
        where TTableRow : ITableRow
        where TInsertedEvent : IRowEvent<TTableRow>
        where TDeletedEvent : IRowEvent<TTableRow>
        where TRestoredEvent : IRowEvent<TTableRow>
        where TUpdatedEvent : IUpdatedRowEvent<TTableRow>
    {
        public EventSourcedRow(EventSourcedMetadata metadata, TTableRow row, bool deleted)
            : base(metadata)
        {
            this.Row = row;
            this.Deleted = deleted;
        }

        public TTableRow Row { get; protected set; }

        public bool Deleted { get; protected set; }

        public Type GetInsertedType() => typeof(TInsertedEvent);
        public Type GetDeletedType() => typeof(TDeletedEvent);
        public Type GetRestoredType() => typeof(TRestoredEvent);
        public Type GetUpdatedType() => typeof(TUpdatedEvent);

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .On<TInsertedEvent>(e => this.Row = e.Row)
                .On<TDeletedEvent>(e => this.Deleted = true)
                .On<TRestoredEvent>(e =>
                {
                    this.Deleted = false;
                    this.Row = e.Row;
                })
                .On<TUpdatedEvent>(e => this.Row = e.Row);
    }
}
