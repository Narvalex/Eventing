using System;

namespace Infrastructure.EventSourcing
{
    public static class TypeObjectExtensions
    {
        public static Type ToClrType(this TypeObject type)
        {
            var result = Type.GetType($"{type.Name}, {type.Assembly}");
            if (result is null)
                throw new ArgumentException($"The type '{type.Name}, {type.Assembly}' is not valid");
            return result;
        }
    }
}
