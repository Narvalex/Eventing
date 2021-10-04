using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Infrastructure.EventSourcing
{
    // Implementation from http://grabbagoft.blogspot.com/2007/06/generic-value-object-equality.html
    // Unit tests can be found there. Otherwise look in Github for Volaks Aggregate.NET
    public abstract class ValueObject<T> : IEquatable<T>
        where T : ValueObject<T>
    {
        private int? _cachedHash;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = obj as T;

            return Equals(other);
        }

        public override int GetHashCode()
        {
            if (_cachedHash.HasValue) return _cachedHash.Value;

            unchecked
            {
                var fields = GetFields();

                const int startValue = 17;
                const int multiplier = 59;

                var hashCode = fields.Select(field => field.GetValue(this)).Where(value => value != null).Aggregate(startValue, (current, value) => (current * multiplier) + value.GetHashCode());

                _cachedHash = hashCode;
            }

            return _cachedHash.Value;
        }

        public virtual IEnumerable<EquatableObjectProperty> GetDiferentFieldValuesOfAnotherObject(T other)
        {
            var list = new List<EquatableObjectProperty>();
            if (other == null)
                return list;

            var t = GetType();
            var otherType = other.GetType();

            if (t != otherType)
                return list;

            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                var thisValue = property.GetValue(this);
                var otherValue = property.GetValue(other);

                if (thisValue is null && otherValue is null)
                    continue;

                if (thisValue is null && otherValue != null)
                    list.Add(new EquatableObjectProperty(property.Name, otherValue));
                else if (!thisValue.Equals(otherValue))
                    list.Add(new EquatableObjectProperty(property.Name, otherValue));
            }

            return list;
        }

        public virtual bool Equals(T other)
        {
            if (other == null)
                return false;

            var t = this.GetType();
            var otherType = other.GetType();

            if (t != otherType)
                return false;

            var fields = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var field in fields)
            {
                var value1 = field.GetValue(other);
                var value2 = field.GetValue(this);

                if (value1 == null)
                {
                    if (value2 != null)
                        return false;
                }
                else if (!value1.Equals(value2))
                {
                    // Support for collections
                    if ((value1 is IEnumerable) && (value2 is IEnumerable))
                    {
                        var collection1 = ((IEnumerable)value1).Cast<object>();
                        var collection2 = ((IEnumerable)value2).Cast<object>();

                        if (!collection1.SequenceEqual(collection2))
                            return false;
                    }
                    else if (value1 is DateTime && value2 is DateTime)
                    {
                        if (((DateTime)value1).ToUniversalTime() != ((DateTime)value2).ToUniversalTime())
                            return false;
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        private IEnumerable<FieldInfo> GetFields()
        {
            var t = GetType();

            var fields = new List<FieldInfo>();

            while (t != typeof(object))
            {
                fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

                t = t.BaseType;
            }

            return fields;
        }

        public static bool operator ==(ValueObject<T> x, ValueObject<T> y)
        {
            // note doing x == null would cause an infinite loop
            return object.ReferenceEquals(x, null) ? object.ReferenceEquals(y, null) : x.Equals(y);
        }

        public static bool operator !=(ValueObject<T> x, ValueObject<T> y)
        {
            return !(x == y);
        }
    }
}
