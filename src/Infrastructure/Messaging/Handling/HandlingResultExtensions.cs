namespace Infrastructure.Messaging.Handling
{
    public static class HandlingResultExtensions
    {
        public static IResponse<T> AsResponse<T>(this IHandlingResult result)
        {
            return (IResponse<T>)result;
        }
    }
}
