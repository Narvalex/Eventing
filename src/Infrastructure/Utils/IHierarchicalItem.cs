using System.Collections.Generic;

namespace Infrastructure.Utils
{
    public interface IHierarchicalItem<T>
    {
        T Item { get;  }
        ICollection<IHierarchicalItem<T>> Children { get; }
    }
}