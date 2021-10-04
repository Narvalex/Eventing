using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public sealed class StrSubEntities<T> : StrSubEntitiesBase<T>, ISubEntities
       where T : StrSubEntityBase
    {
        private bool handlersAreRegistered = false;
        private IHandlerRegistry eventSourced = null;

        public StrSubEntities(InterceptedDictionary<string, T>? list = null)
             : base(list)
        {
        }

        void ISubEntities.OnRegisteringHandlers(IHandlerRegistry eventSourced)
        {
            // Some entities have constructor validations, that is why we cant do the following
            // var entitySample = ObjectCreator.New<T>();

            if (this.List.Any())
                this.RegisterHandlers(this.List.Values.First(), eventSourced);
            else
                this.eventSourced = eventSourced;
        }


        public StrSubEntities<T> Add(T entity)
        {
            this.List.Add(entity.Id, entity);
            return this;
        }

        public StrSubEntities<T> AddRange(IEnumerable<T> entity)
        {
            this.List.AddRange(entity.Select(x => new KeyValuePair<string, T>(x.Id, x)));
            return this;
        }

        public StrSubEntities<T> Remove(string id)
        {
            this.List.Remove(id);
            return this;
        }

        public StrSubEntities<T> ReorderBy<TKeySelector>(Func<T, TKeySelector> func)
        {
            var enumerable = this.List.Values.OrderBy(func);
            this.List = new InterceptedDictionary<string, T>();
            this.AddRange(enumerable);
            return this;
        }

        public StrSubEntities<T> ReorderByDesc<TKeySelector>(Func<T, TKeySelector> func)
        {
            var enumerable = this.List.Values.OrderByDescending(func);
            this.List = new InterceptedDictionary<string, T>();
            this.AddRange(enumerable);
            return this;
        }

        protected override void OnAddingEntity(string entityId)
        {
            if (!this.handlersAreRegistered)
                this.RegisterHandlers(this.List.Values.First(), this.eventSourced);
        }

        private void RegisterHandlers(T entitySample, IHandlerRegistry eventSourced)
        {
            entitySample.Handlers.ForEach(pair =>
            {
                eventSourced.On(pair.Key, x =>
                {
                    var id = pair.Value.idSelector(x);
                    this.List[id].InvokeHandler(pair.Key, x);
                });
            });
            entitySample.IgnoredEvents.ForEach(x => eventSourced.On(x));

            this.handlersAreRegistered = true;
        }
    }
}
