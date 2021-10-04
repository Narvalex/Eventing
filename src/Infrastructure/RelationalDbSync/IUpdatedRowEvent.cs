using Infrastructure.EventSourcing;
using System.Collections.Generic;

namespace Infrastructure.RelationalDbSync
{
    public interface IUpdatedRowEvent<T> : IRowEvent<T> where T : ITableRow
    {
        IEnumerable<EquatableObjectProperty> UpdatedFields { get; }
    }
}
