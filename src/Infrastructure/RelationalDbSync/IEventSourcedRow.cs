using Infrastructure.EventSourcing;
using System;

namespace Infrastructure.RelationalDbSync
{
    public interface IEventSourcedRow<TTableRow> : IEventSourced where TTableRow : ITableRow
    {
        TTableRow Row { get; }
        bool Deleted { get; }
        public Type GetInsertedType();
        public Type GetDeletedType();
        public Type GetRestoredType();
        public Type GetUpdatedType();
    }

    public interface IEventSourcedRow<TTableRow, TInsertedEvent, TDeletedEvent, TRestoredEvent, TUpdatedEvent> : IEventSourcedRow<TTableRow>
        where TTableRow : ITableRow
        where TInsertedEvent : IRowEvent<TTableRow>
        where TDeletedEvent : IRowEvent<TTableRow>
        where TRestoredEvent : IRowEvent<TTableRow>
        where TUpdatedEvent : IUpdatedRowEvent<TTableRow>
    {

    }
}
