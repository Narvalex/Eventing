using System;
using System.IO;
using System.Threading.Tasks;

namespace Eventing.Client.Http
{
    public interface IHttpLite
    {
        Task<Stream> Get(string uri, string token = null);
        Task<TResult> Get<TResult>(string uri, string token = null);
        Task<TResult> Post<TContent, TResult>(string uri, TContent content, string token = null);
        Task Post(string uri, string jsonContent, string token = null);
        Task Post<TContent>(string uri, TContent content, string token = null);
        Task<TResult> Post<TResult>(string uri, string jsonContent, string token = null);
        Task Upload(string uri, Stream fileStream, string fileName, string metadatos, string token = null);
        Task<TResult> Upload<TResult>(string uri, Stream fileStream, string fileName, string metadatos, string token = null);
    }

    public class RemoteUnauthrorizedResponseException : Exception
    {
        public RemoteUnauthrorizedResponseException()
        { }

        public RemoteUnauthrorizedResponseException(string message) : base(message)
        { }
    }

    public class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException()
        { }

        public ServiceUnavailableException(string message) : base(message)
        { }
    }
}