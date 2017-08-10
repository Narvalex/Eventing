namespace Eventing.OfflineClient
{
    public interface IDurablePendingMessageQueue : IPendingMessagesQueue
    {
        bool DatabseExists { get; }

        void CreateDbIfNotExists();

        void DropDb();
    }
}
