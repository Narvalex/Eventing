namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// A mutex saga is a globally unique coordinator for a process started by a command. A normal saga can be created 
    /// in concurrecy, but a mutex instance is only created once per type. This allows the enforcement of certain 
    /// invariants or rules to be applied in a one command at a time fashion. 
    /// 
    /// If multiple commands are issued to a mutex saga, the first request will win, and the 
    /// others will be enqueued in the saga itself in orther to be handled in strict order.
    /// 
    /// Another important aspect is that its correlation id is not the same for all processes, 
    /// it is still one id per process-initiator-command.
    /// </summary>
    public interface IMutexSagaExecutionCoordinator : ISagaExecutionCoordinator { }
}
