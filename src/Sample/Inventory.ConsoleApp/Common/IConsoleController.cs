namespace Inventory.ConsoleApp
{
    public interface IConsoleController
    {
        bool TryHandle(string cmd);
        string Description { get; }
    }
}
