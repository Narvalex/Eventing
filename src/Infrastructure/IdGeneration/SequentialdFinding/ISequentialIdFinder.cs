using System.Threading.Tasks;

namespace Infrastructure.IdGeneration
{
    /// <summary>
    /// Finds the next is for a stream. May threads can compete for the same Id, 
    /// as oposed to <see cref="ISequentialIdGenerator"/>. This just looks for the next number in 
    /// the stream. This is the recommeded approach for single-simple transactions.
    /// </summary>
    /// <remarks>
    /// The generic T parameter is not necessary, but prevents from passing around an 
    /// invalid implementation of the finder to a command handler.
    /// </remarks>
    public interface ISequentialIdFinder<T>
    {
        Task<string> NextAsync();
    }
}
