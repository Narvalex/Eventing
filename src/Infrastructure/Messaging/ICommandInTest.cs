namespace Infrastructure.Messaging
{
    public interface ICommandInTest : ICommandInTransit
    {
        void SetCommandId(string commandId);
        void SetCausationId(string causationId);
    }
}
