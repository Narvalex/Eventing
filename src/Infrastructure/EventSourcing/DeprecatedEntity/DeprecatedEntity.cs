using System;

namespace Infrastructure.EventSourcing
{
    [Obsolete]
    public abstract class DeprecatedEntity<TId, TEntity> : IEquatable<TEntity> where TEntity : DeprecatedEntity<TId, TEntity>
    {
        protected DeprecatedEntity(TId id)
        {
            this.Id = id;
        }

        public virtual TId Id { get; }

        public override bool Equals(object other) { return Equals(other as TEntity); }
        public bool Equals(TEntity other)
        {
            if (other == null)
                return false;

            return this.Id.Equals(other.Id);
        }

        public static bool operator ==(DeprecatedEntity<TId, TEntity> left, DeprecatedEntity<TId, TEntity> right)
            => Equals(left, right);

        public static bool operator !=(DeprecatedEntity<TId, TEntity> left, DeprecatedEntity<TId, TEntity> right)
            => !(left == right);

        public override int GetHashCode()
            => 17 * 31 + (this.Id == null ? 0 : this.Id.GetHashCode());
    }
}
