using Infrastructure.Messaging.Handling;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Tests.Messaging.Handling.Mocks
{
    public class EventHandlerMock : IEventHandler<FooEvent>, IEventHandler<BarEvent>
    {
        private Exception failException;
        private bool fail = false;

        public async Task Handle(FooEvent e)
        {
            this.FooEventHandlingStartedCount++;
            if (fail) throw this.failException;

            this.FooEventHandled = true;
            await Task.CompletedTask;
        }

        public async Task Handle(BarEvent e)
        {
            this.BarEventHandlingStartedCount++;
            if (fail) throw this.failException;

            this.BarEventHandled = true;
            await Task.CompletedTask;
        }

        public void ThrowOnHandling(Exception ex)
        {
            this.failException = ex;
            this.fail = true;
        }

        public bool FooEventHandled { get; private set; }
        public bool BarEventHandled { get; private set; }

        public int FooEventHandlingStartedCount { get; private set; }
        public int BarEventHandlingStartedCount { get; private set; }
    }
}
