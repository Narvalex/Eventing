using Infrastructure.Messaging;
using System.Threading.Tasks;

namespace Infrastructure.IdGeneration
{
    /// <summary>
    /// Generates a new Id for a stream. Only one thread con obtain an Id for its process, as 
    /// oposed to <see cref="ISequentialIdFinder"/>. This component emits an event per number 
    /// generated. This is useful for Sagas, and for composite id. Eg. person-1_product-45_other-3445
    /// </summary>
    /// <remarks>
    /// The T paramter is not necessary, but is useful to validate the correct dependency.
    /// </remarks>
    public interface ISequentialIdGenerator<T>
    {
        Task<string> NewAsync(ICommand command);

        Task<string> NewAsync(IEvent @event);
    }
}
