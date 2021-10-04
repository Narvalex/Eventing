namespace Infrastructure.EventSourcing
{
    public class EquatableObjectProperty
    {
        public EquatableObjectProperty(string name, object? value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; }
        public object? Value { get; }
    }
}
