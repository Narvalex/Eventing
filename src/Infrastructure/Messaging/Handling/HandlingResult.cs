namespace Infrastructure.Messaging.Handling
{
    // This only applies to commands
    public class HandlingResult : IHandlingResult
    {
        public HandlingResult(bool success, params string[] messages)
        {
            this.Success = success;
            this.Messages = messages;
        }

        public bool Success { get; }
        public string[] Messages { get; }

        public static HandlingResult SuccessResult() => new HandlingResult(true);
    }
}
