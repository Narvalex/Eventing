using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public sealed class SubEntities<T> : SubEntitiesBase<T>, ISubEntities
        where T : SubEntityBase
    {
        private bool handlersAreRegistered = false;
        private IHandlerRegistry eventSourced = null!;

        public SubEntities(int lastId = 0, InterceptedDictionary<int, T>? list = null)
             : base(lastId, list)
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


        public SubEntities<T> Add(T entity)
        {
            this.List.Add(entity.Id, entity);
            return this;
        }

        public SubEntities<T> AddRange(IEnumerable<T> entity)
        {
            this.List.AddRange(entity.Select(x => new KeyValuePair<int, T>(x.Id, x)));
            return this;
        }

        public SubEntities<T> Remove(int id)
        {
            this.List.Remove(id);
            return this;
        }

        public SubEntities<T> Remove(T entity)
        {
            this.List.Remove(entity.Id);
            return this;
        }

        public SubEntities<T> Clear()
        {
            this.SetupDictionary(new InterceptedDictionary<int, T>());
            return this;
        }

        public SubEntities<T> ReorderBy<TKeySelector>(Func<T, TKeySelector> func)
        {
            var enumerable = this.List.Values.OrderBy(func);
            this.SetupDictionary(new InterceptedDictionary<int, T>());
            this.AddRange(enumerable);
            return this;
        }

        public SubEntities<T> ReorderByDesc<TKeySelector>(Func<T, TKeySelector> func)
        {
            var enumerable = this.List.Values.OrderByDescending(func);
            this.SetupDictionary(new InterceptedDictionary<int, T>());
            this.AddRange(enumerable);
            return this;
        }

        protected override void OnAddingEntity(int entityId)
        {
            base.OnAddingEntity(entityId);

            if (!this.handlersAreRegistered)
                this.RegisterHandlers(this.List.Values.First(), this.eventSourced);
        }

        public List<T> ToList() => this.List.Values.ToList();

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
