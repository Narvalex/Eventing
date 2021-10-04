using System;

namespace Infrastructure.Snapshotting
{
    public static class SnapshotSchemaExtensions
    {
        public static Type GetSnapshotType(this SnapshotSchema schema)
        {
            var type = Type.GetType($"{schema.Type}, {schema.Assembly}");
            if (type is null)
                throw new InvalidOperationException("The schema has an invalid snapshot type");
            return type;
        }
    }
}
