namespace Inventory.ConsoleApp
{
    public interface IConsoleController
    {
        bool TryHandle(string cmd);
        string Description { get; }
        string Key { get; }
        string Args { get; }
    }
}
