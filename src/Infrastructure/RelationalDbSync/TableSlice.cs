namespace Infrastructure.RelationalDbSync
{
    public class TableSlice<T> where T : ITableRow
    {
        public TableSlice(TableSliceFetchStatus status, bool isEndOfTable, int nextRowNumber, T[] rows)
        {
            this.Status = status;
            this.IsEndOfTable = isEndOfTable;
            this.NextRowNumber = nextRowNumber;
            this.Rows = rows;
        }

        public TableSliceFetchStatus Status { get; }
        public bool IsEndOfTable { get; }
        public int NextRowNumber { get; }
        public T[] Rows { get; }
    }
}
