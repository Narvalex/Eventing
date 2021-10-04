using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface IOnQueryFailed
    {
        /// <summary>
        /// When a query faild, can optionaly return a <see cref="IHandlingResult"/>, or just fail without giving a clue to the client.
        /// </summary>
        Task<IHandlingResult> OnQueryFailed(IQuery query, Exception exception);
    }
}
