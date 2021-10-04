using Infrastructure.EventSourcing;

namespace Infrastructure.Messaging
{
    public interface IForeignKeyRegistry
    {
        IForeignKeyRegistry Register<T>(string streamId) where T : IEventSourced;
        IForeignKeyRegistry Register<T, U>(string streamId) where T : IEventSourced where U : IEventSourced;
    }
}
