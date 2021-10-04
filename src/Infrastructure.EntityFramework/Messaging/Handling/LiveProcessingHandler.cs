using Infrastructure.Utils;
using System;

namespace Infrastructure.EntityFramework.Messaging.Handling
{
    public class LiveProcessingHandler
    {
        private long lastReceived = -1;
        private readonly Action handler;
        private bool firstTime = true;
        private readonly int eventCountThreshold;

        public LiveProcessingHandler(Action handler, int eventCountThreshold = 10)
        {
            this.handler = Ensured.NotNull(handler, nameof(handler));
            this.eventCountThreshold = Ensured.Positive(eventCountThreshold, nameof(eventCountThreshold));
        }

        public void EnterLiveProcessingIfApplicable(long lastFromStream)
        {
            if (this.firstTime)
            {
                this.firstTime = false;
                this.EnterLiveProcessing(lastFromStream);
                return;
            }

            if (lastFromStream > this.lastReceived && (lastFromStream - this.lastReceived) > this.eventCountThreshold)
            {
                // we where far behind stream
                this.EnterLiveProcessing(lastFromStream);
            }
        }

        private void EnterLiveProcessing(long lastFromStream)
        {
            this.lastReceived = lastFromStream;
            this.handler.Invoke();
        }
    }
}
