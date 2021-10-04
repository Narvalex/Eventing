namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// An event sourced SEC.
    /// Remember: Always override the correlation id in a <see cref="Messaging.Command"/> in order to 
    /// continue with the same correlation id that started it all. But do not do this with a 
    /// <see cref="IMutexSagaExecutionCoordinator"/>.
    /// </summary>
    public interface ISagaExecutionCoordinator : IEventSourced { }
}
