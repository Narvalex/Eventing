using Infrastructure.Messaging;

namespace Infrastructure.RelationalDbSync
{
    public interface IRowEvent
    {
        string TableName { get; }
    }

    public interface IRowEvent<T> : IRowEvent, IEventInTransit where T : ITableRow
    {
        T Row { get; }
    }
}
