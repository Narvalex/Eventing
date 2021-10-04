using System.Collections.Generic;

namespace Infrastructure.Utils
{
    internal class HierarchicalItem<T> : IHierarchicalItem<T>
    {
        public T Item { get; set; }
        public ICollection<IHierarchicalItem<T>> Children { get; set; }
    }
}
