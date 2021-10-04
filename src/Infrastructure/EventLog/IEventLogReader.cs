using Infrastructure.EventStorage;
using System.Threading.Tasks;

namespace Infrastructure.EventLog
{
    public interface IEventLogReader 
    {
        Task<CategoryStreamsSlice> GetCategoryStreamsSliceAsync(string category, long from, int count);

        Task<CategoryStreamsSlice> GetCategoryStreamsSliceAsync(string category, long from, int count, long maxEventNumber);

        Task<string?> ReadLastStreamFromCategory(string category, int offset = 0);
    }
}
