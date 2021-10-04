using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Messaging.Handling
{
    public class EventProcessorId : IEquatable<EventProcessorId?>
    {
        public EventProcessorId(params IReadModelProjection[] projections)
        {
            var readModelName = Ensured.NotEmpty(projections.First().ReadModelName, "ReadModelName");
            var isEventLog = false;
            projections.ForEach(x =>
            {
                if (x.ReadModelName != readModelName)
                    throw new InvalidOperationException("Not all read model projections generates the same model");

                var handlerName = x.GetType().Name;
                if (handlerName.EndsWith("EventLogProjection"))
                    isEventLog = true;
                else if (!(handlerName.EndsWith("ReadModelProj") || handlerName.EndsWith("ReadModelProjection")))
                    throw new InvalidOperationException(
                        $"The read model projection {handlerName} has an invalid name. It should end with either 'ReadModelProj' or 'ReadModelProjection'");
            });

            this.SubscriptionName = Ensured.NotEmpty(readModelName, "ReadModelName");
            this.Type = EventProcessorType.ReadModelProjection;
            this.TypeDescription = EventProcessorConsts.ReadModelProjection;
            this.IsEventLog = isEventLog;
        }

        public EventProcessorId(IEventHandler handler)
        {
            if (handler is IReadModelProjection)
                throw new ArgumentException("Invalid handler for this constructor", nameof(handler));

            this.SubscriptionName = handler.GetType().Name;

            if (this.SubscriptionName.EndsWith("EventHandler")
                || this.SubscriptionName.EndsWith("EvHandler"))
            {
                this.Type = EventProcessorType.EventHandler;
                this.TypeDescription = EventProcessorConsts.EventHandler;
            }
            else if (this.SubscriptionName.EndsWith("PersistentCommandHandler")
                || this.SubscriptionName.EndsWith("PersistentCmdHandler"))
            {
                this.Type = EventProcessorType.PersistentCommandHandler;
                this.TypeDescription = EventProcessorConsts.PersistentCommandHandler;
            }
            else if (this.SubscriptionName.EndsWith("EmailSender"))
            {
                this.Type = EventProcessorType.EmailSender;
                this.TypeDescription = EventProcessorConsts.EmailSender;
            }
            else
            {
                throw new InvalidOperationException($"The handler {this.SubscriptionName} has an invalid name");
            }

            this.IsEventLog = false;
        }

        public EventProcessorId(string subscriptionName, string typeDescription)
        {
            this.SubscriptionName = subscriptionName;
            this.TypeDescription = typeDescription;

            switch (typeDescription)
            {
                case EventProcessorConsts.EventHandler:
                    this.Type = EventProcessorType.EventHandler;
                    break;

                case EventProcessorConsts.PersistentCommandHandler:
                    this.Type = EventProcessorType.PersistentCommandHandler;
                    break;

                case EventProcessorConsts.ReadModelProjection:
                    this.Type = EventProcessorType.ReadModelProjection;
                    break;

                case EventProcessorConsts.EmailSender:
                    this.Type = EventProcessorType.EmailSender;
                    break;

                default:
                    throw new InvalidOperationException("Invalid type description");
            }

            if (subscriptionName == "EventLog" && typeDescription == EventProcessorConsts.ReadModelProjection)
                this.IsEventLog = true;
        }

        public string SubscriptionName { get; }
        public EventProcessorType Type { get; }
        public string TypeDescription { get; }
        public bool IsEventLog { get; }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as EventProcessorId);
        }

        public bool Equals(EventProcessorId? other)
        {
            return other != null &&
                   this.SubscriptionName == other.SubscriptionName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.SubscriptionName);
        }

        public static bool operator ==(EventProcessorId? left, EventProcessorId? right) => EqualityComparer<EventProcessorId>.Default.Equals(left, right);
        public static bool operator !=(EventProcessorId? left, EventProcessorId? right) => !(left == right);
    }
}
