using Infrastructure.EventSourcing;
using System.Collections.Generic;

namespace Infrastructure.RelationalDbSync
{
    public abstract class UpdatedRowEvent<T> : RowEvent<T>, IUpdatedRowEvent<T> where T : ITableRow
    {
        public UpdatedRowEvent(T row, IEnumerable<EquatableObjectProperty> updatedFields) 
            : base(row)
        {
            UpdatedFields = updatedFields;
        }

        public IEnumerable<EquatableObjectProperty> UpdatedFields { get; }
    }
}
