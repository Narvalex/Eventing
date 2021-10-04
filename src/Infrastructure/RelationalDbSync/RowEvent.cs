using Infrastructure.Messaging;

namespace Infrastructure.RelationalDbSync
{
    public abstract class RowEvent<T> : Event, IRowEvent<T> where T : ITableRow
    {
        public RowEvent(T row)
        {
            Row = row;
        }

        public T Row { get; }
        public override string StreamId => this.Row.Key;

        string IRowEvent.TableName => this.ResolvedTableName;

        protected abstract string ResolvedTableName { get; }
    }
}
