using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public static class DeprecatedEntityExtensions
    {
        public static TEntity Find<TId, TEntity>(this IEnumerable<TEntity> collection, TId id) where TEntity : DeprecatedEntity<TId, TEntity>
            => collection.FirstOrDefault(x => x.Id.Equals(id));
    }
}
