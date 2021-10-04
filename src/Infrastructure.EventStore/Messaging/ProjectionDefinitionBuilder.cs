using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using Infrastructure.EventSourcing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.EventStore.Messaging
{
    public class ProjectionDefinitionInitBuilder
    {
        internal ProjectionDefinitionInitBuilder(string projectionName, string emittedStream, ProjectionsManager manager, UserCredentials credentials)
        {
            this.ProjectionName = projectionName;
            this.EmittedStream = emittedStream;
            this.ProjectionManager = manager;
            this.Credentials = credentials;
        }

        internal string ProjectionName { get; }
        internal string EmittedStream { get; }
        internal ProjectionsManager ProjectionManager { get; }
        internal UserCredentials Credentials { get; }

        public ProjectionDefinitionBuilder From<T>() where T : class, IEventSourced
        {
            return new ProjectionDefinitionBuilder(this, EventStream.GetCategory<T>());
        }

        // We do not create many projections as they may pollute the event store.
        public ProjectionDefinition From(ICollection<string> categories)
        {
            return new ProjectionDefinition(this.ProjectionManager, this.Credentials, this.ProjectionName, this.EmittedStream, categories);
        }
    }

    public class ProjectionDefinitionBuilder
    {
        private readonly ProjectionDefinitionInitBuilder init;
        private readonly List<string> streams;

        internal ProjectionDefinitionBuilder(ProjectionDefinitionInitBuilder init, string stream)
        {
            this.init = init;
            this.streams = new List<string>();
            this.streams.Add(stream);
        }

        public ProjectionDefinitionBuilder And<T>() where T : class, IEventSourced
        {
            var stream = EventStream.GetCategory<T>();
            if (this.streams.Any(x => x == stream)) throw new ArgumentException("The stream was already registered!");
            this.streams.Add(stream);
            return this;
        }

        public ProjectionDefinition AndNothingMore()
            => new ProjectionDefinition(this.init.ProjectionManager, this.init.Credentials, this.init.ProjectionName, this.init.EmittedStream, this.streams);
    }
}
