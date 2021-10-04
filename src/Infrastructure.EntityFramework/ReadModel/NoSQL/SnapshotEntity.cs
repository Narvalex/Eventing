using Infrastructure.EventSourcing;
using System;

namespace Infrastructure.EntityFramework.ReadModel.NoSQL
{
    public class SnapshotEntity<T> : DocumentEntityBase<T>, ISnapshotEntity
        where T : class, IEventSourced
    {
        public string StreamId { get; set; } = null!;
        public long Version { get; set; }
        public int SchemaVersion { get; set; }

        public override T Open()
        {
            throw new InvalidOperationException("Can not open directly an snapshot entity");
        }

        internal T InternalOpen() => base.Open();
    }
}
