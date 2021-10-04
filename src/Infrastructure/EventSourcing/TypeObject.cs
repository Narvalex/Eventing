namespace Infrastructure.EventSourcing
{
    public class TypeObject : ValueObject<TypeObject>
    {
        public TypeObject(string name, string assembly)
        {
            this.Name = name;
            this.Assembly = assembly;
        }

        public string Name { get; }
        public string Assembly { get; }
    }
}
