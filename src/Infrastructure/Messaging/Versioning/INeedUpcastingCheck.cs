namespace Infrastructure.Messaging.Versioning
{
    /// <summary>
    /// Marker interface thant hints the <see cref="EventDeserializationAndVersionManager"/> that the implementor 
    /// may need an upcasting
    /// </summary>
    public interface INeedUpcastingCheck { }
}
