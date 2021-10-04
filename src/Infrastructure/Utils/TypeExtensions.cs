using Infrastructure.EventSourcing;
using System;

namespace Infrastructure.Utils
{
    public static class TypeExtensions
    {
        public static TypeObject ToTypeObject(this Type type) =>
            new TypeObject(type.FullName!, type.Assembly.GetName().Name!);
    }
}
