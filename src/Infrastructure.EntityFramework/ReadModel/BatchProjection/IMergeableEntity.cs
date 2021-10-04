using System;

namespace Infrastructure.EntityFramework.ReadModel.BatchProjection
{
    public interface IMergeableEntity<T> where T : IMergeableEntity<T>
    {
        Func<T, T, bool> MergePredicate { get; }
    }
}
