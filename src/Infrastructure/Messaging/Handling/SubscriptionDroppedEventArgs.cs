using System;

namespace Infrastructure.Messaging.Handling
{
    public class SubscriptionDroppedEventArgs : EventArgs
    {
        public SubscriptionDroppedEventArgs(Exception exception, bool lostConnectionReason)
        {
            this.Exception = exception;
            this.LostConnectionReason = lostConnectionReason;
        }

        public Exception Exception { get; }
        public bool LostConnectionReason { get; }
    }
}
