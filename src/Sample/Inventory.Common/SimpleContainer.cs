using System;
using System.Collections.Generic;

namespace Inventory.Common
{
    public class SimpleContainer
    {
        private IDictionary<Type, object> singletons = new Dictionary<Type, object>();
        private IDictionary<Type, Func<object>> factories = new Dictionary<Type, Func<object>>();

        public void Register<T>(Func<T> factory)
        {
            this.factories[typeof(T)] = () => factory.Invoke();
        }

        public void Register<T>(T instance)
        {
            this.singletons[typeof(T)] = instance;
        }

        public T ResolveNewOf<T>()
        {
            var type = typeof(T);
            if (!this.factories.ContainsKey(type))
                throw new FactoryMethodNotFoundException(type.Name);

            return (T)this.factories[type].Invoke();
        }

        public T ResolveSingleton<T>()
        {
            var type = typeof(T);
            if (!this.singletons.ContainsKey(type))
                throw new DependencyNotFoundException(type.Name);

            return (T)this.singletons[type];
        }
    }

    public class DependencyNotFoundException : Exception
    {
        public DependencyNotFoundException(string dependencyType)
            : base($"The dependency of type {dependencyType} could not be found in the simple container")
        { }
    }

    public class FactoryMethodNotFoundException : Exception
    {
        public FactoryMethodNotFoundException(string type)
            : base($"The factory method of type {type} was not found in the simple container")
        { }
    }
}
