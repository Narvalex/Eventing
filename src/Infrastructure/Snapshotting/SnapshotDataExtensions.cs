using System;

namespace Infrastructure.Snapshotting
{
    public static class SnapshotDataExtensions
    {
        public static Type GetSnapshotType(this SnapshotData snapshotData)
        {
            var type = Type.GetType($"{snapshotData.Type}, {snapshotData.Assembly}");
            if (type is null)
                throw new InvalidOperationException("The snapshot data has an invalid snapshot type");
            return type;
        }
    }
}
