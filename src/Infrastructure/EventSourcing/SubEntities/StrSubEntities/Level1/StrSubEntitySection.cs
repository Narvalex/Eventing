namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// "Marker type" to make more explicit a section in an entity.
    /// </summary>
    public abstract class StrSubEntitySection : StrSubEntityBase
    {
        protected StrSubEntitySection(string id) : base(id)
        {
        }
    }
}
