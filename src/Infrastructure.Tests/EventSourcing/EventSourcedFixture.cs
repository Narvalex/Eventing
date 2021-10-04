using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using Xunit;

namespace Infrastructure.Tests.EventSourcing
{
    public class when
    {
        // Required stream name: {streamCategory}-{streamId}
        public when()
        {
            EventSourced.SetValidNamespace("Infrastructure.Tests.EventSourcing");
        }

        [Fact]
        public void saving_entity_without_id_then_throws()
        {
            var entity = EventSourcedCreator.New<EntityWithoutId>();
            Assert.Throws<ArgumentException>(() => entity.Update("corrId", "causId", null, new MessageMetadata("test123", "TestGentleman", "localhost", "Internet Exporer 6, WinVista"),
                true,
                new EventA(null)));
        }

        [Fact]
        public void emiting_entity_with_null_category_then_does_not_throw()
        {
            var entity = EventSourcedCreator.New<EntityWithNullCategory>();
            entity.Update("corrId", "causId", null, new MessageMetadata("test123", "TestGentleman", "localhost", "Internet Exporer 6, WinVista"),
                true,
                new EventA("2"));
        }

        [Fact]
        public void emiting_entity_with_emtpty_category_then_does_not_throw()
        {
            var entity = EventSourcedCreator.New<EntityWitEmptyCategory>();
            entity.Update("corrId", "causId", null, new MessageMetadata("test123", "TestGentleman", "localhost", "Internet Exporer 6, WinVista"),
                true,
                new EventA("2"));
        }


        internal class EntityWithoutId : EventSourced
        {
            public EntityWithoutId(EventSourcedMetadata metadata) 
                : base(metadata)
            {
            }

            protected override void OnRegisteringHandlers(IHandlerRegistry registry)
            {

            }
        }

        internal class EntityWithNullCategory : EventSourced
        {
            public EntityWithNullCategory(EventSourcedMetadata metadata) 
                : base(metadata)
            {
            }

            protected override void OnRegisteringHandlers(IHandlerRegistry registry)
            {
                registry.On<EventA>(_ => { });
            }
        }

        internal class EntityWithWhiteSpaceInCategory : EventSourced
        {
            public EntityWithWhiteSpaceInCategory(EventSourcedMetadata metadata) 
                : base(metadata)
            {
            }

            protected override void OnRegisteringHandlers(IHandlerRegistry registry)
            {

            }
        }

        internal class EntityWithCategoryWithSpace : EventSourced
        {
            public EntityWithCategoryWithSpace(EventSourcedMetadata metadata) 
                : base(metadata)
            {
            }

            protected override void OnRegisteringHandlers(IHandlerRegistry registry)
            {

            }
        }

        internal class EntityWitEmptyCategory : EventSourced
        {
            public EntityWitEmptyCategory(EventSourcedMetadata metadata) 
                : base(metadata)
            {
            }

            protected override void OnRegisteringHandlers(IHandlerRegistry registry)
            {
                registry.On<EventA>(_ => { });
            }
        }

        internal class EventA : Event
        {
            public EventA(string streamId)
            {
                this.StreamId = streamId;
            }

            public override string StreamId { get; }
        }
    }
}
