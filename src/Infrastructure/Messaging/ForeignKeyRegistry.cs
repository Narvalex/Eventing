using Infrastructure.EventSourcing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    internal class ForeignKeyRegistry : IForeignKeyRegistry
    {
        private Queue<((Type t, Type u) types, string streamId)>? doubleDiscriminatedForeignKeys;
        private bool hasDoubleDiscriminatedFKs = false;

        private Queue<(Type type, string streamId)>? foreignKeys;
        private bool hasForeignKeys = false;

        IForeignKeyRegistry IForeignKeyRegistry.Register<T>(string streamId)
        {
            // Lazy dictionary creation, to not penalize events without FK
            if (!this.hasForeignKeys)
            {
                foreignKeys = new Queue<(Type type, string streamId)>();
                this.hasForeignKeys = true;
            }

            foreignKeys!.Enqueue((typeof(T), streamId));
            return this;
        }

        IForeignKeyRegistry IForeignKeyRegistry.Register<T, U>(string streamId)
        {
            // Lazy dictionary creation, to not penalize events without FK
            if (!this.hasDoubleDiscriminatedFKs)
            {
                doubleDiscriminatedForeignKeys = new Queue<((Type t, Type u) types, string streamId)>();
                this.hasDoubleDiscriminatedFKs = true;
            }

            doubleDiscriminatedForeignKeys!.Enqueue(((typeof(T), typeof(U)), streamId));
            return this;
        }

        internal async Task CheckForeingKeysContraints(IEventSourcedReader reader, IEvent @event)
        {
            if (this.hasForeignKeys)
            {
                while (this.foreignKeys!.TryDequeue(out var fk))
                {
                    if (!await reader.ExistsAsync(fk.type, EventStream.GetStreamName(fk.type, fk.streamId)))
                        throw new ForeignKeyViolationException($"The event {@event.GetType()} requires a not found key. Missing key {fk.streamId} from {fk.type.Name}");
                }
                this.hasForeignKeys = false;
            }

            if (this.hasDoubleDiscriminatedFKs)
            {
                while (this.doubleDiscriminatedForeignKeys!.TryDequeue(out var fk))
                {
                    if (!await reader.ExistsAsync(fk.types.t, EventStream.GetStreamName(fk.types.t, fk.streamId)))
                        if (!await reader.ExistsAsync(fk.types.u, EventStream.GetStreamName(fk.types.u, fk.streamId)))
                            throw new ForeignKeyViolationException($"The event {@event.GetType()} requires a not found key. Missing key {fk.streamId} from {fk.types.t.Name} or {fk.types.u.Name}");
                }

                this.hasDoubleDiscriminatedFKs = false;
            }
        }
    }
}
