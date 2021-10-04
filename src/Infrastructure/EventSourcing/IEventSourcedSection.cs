namespace Infrastructure.EventSourcing
{
    public interface IEventSourcedSection
    {
        void SetRoot(IEventSourced eventSourced);
    }
}
