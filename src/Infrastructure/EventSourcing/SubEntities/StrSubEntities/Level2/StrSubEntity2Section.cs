namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// "Marker type" to make more explicit a section in an sub entity.
    /// </summary>
    public abstract class StrSubEntity2Section : StrSubEntity2Base
    {
        protected StrSubEntity2Section(string id) : base(id)
        {
        }
    }
}
