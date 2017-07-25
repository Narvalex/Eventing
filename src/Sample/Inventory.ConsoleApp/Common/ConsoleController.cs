using Eventing;

namespace Inventory.ConsoleApp.Common
{
    public abstract class ConsoleController : IConsoleController
    {
        protected readonly string key;

        public string Description { get; }

        public ConsoleController(string key, string description)
        {
            Ensure.NotNullOrWhiteSpace(key, nameof(key));
            Ensure.NotNullOrWhiteSpace(description, nameof(description));

            this.key = key;
            this.Description = description;
        }

        public bool TryHandle(string cmd)
        {
            if (cmd.Equals(this.key, System.StringComparison.OrdinalIgnoreCase))
            {
                this.Handle(null);
                return true;
            }
            else return false;
        }

        public abstract void Handle(string[] args);
    }
}
