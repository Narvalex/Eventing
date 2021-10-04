using System.Threading.Tasks;

namespace Infrastructure.EventLog
{
    public interface IWaitForEventLogToBeConsistent
    {
        Task WaitForEventLogToBeConsistentToCommitPosition(long commitPosition);
        Task WaitForEventLogToBeConsistentToEventNumber(long eventNumber);
    }
}
