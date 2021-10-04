namespace Infrastructure.EventStore.Messaging
{
    public class SubscriptionCheckpoint
    {
        public const string eventTypeName = "subscriptionCheckpoint";

        public SubscriptionCheckpoint(long? eventNumber)
        {
            this.EventNumber = eventNumber;
        }

        /// <summary>
        /// The event number of the last processed event. If it is null 
        /// the subscription will start from the begining.
        /// </summary>
        public long? EventNumber { get; }
    }
}
