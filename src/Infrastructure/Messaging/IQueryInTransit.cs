namespace Infrastructure.Messaging
{
    public interface IQueryInTransit : IQuery
    {
        void SetMetadata(IMessageMetadata metadata);
    }
}
