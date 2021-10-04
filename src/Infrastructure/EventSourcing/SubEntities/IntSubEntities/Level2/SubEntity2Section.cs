namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// "Marker type" to make more explicit a section in an sub entity.
    /// </summary>
    public abstract class SubEntity2Section : SubEntity2Base
    {
        protected SubEntity2Section(int id) : base(id)
        {
        }
    }
}
