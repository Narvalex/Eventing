namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// "Marker type" to make more explicit a section in an entity.
    /// No need to inherit from <see cref="SubEntity{T}"/> as for now.
    /// </summary>
    public abstract class SubEntitySection : SubEntityBase
    {
        protected SubEntitySection(int id) : base(id)
        {
        }
    }
}
