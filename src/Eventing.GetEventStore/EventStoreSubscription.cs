﻿using Eventing.Core.Persistence;
using Eventing.Core.Serialization;
using Eventing.Log;
using EventStore.ClientAPI;
using System;
using System.Text;
using System.Threading;

namespace Eventing.GetEventStore
{
    public class EventStoreSubscription : IEventSubscription
    {
        private ILogLite log = LogManager.GetLoggerFor<EventStoreSubscription>();

        private readonly IEventStoreConnection resilientConnection;
        private readonly IJsonSerializer serializer;
        private readonly string streamName;
        private readonly Action<long, object> handler;
        private readonly bool shouldPersistCheckpoint;
        private readonly Action<long> persistCheckpoint;

        private Lazy<long?> lazyLastCheckpoint;
        private EventStoreCatchUpSubscription subscription;

        private bool shouldStopNow = false;
        private long? lastCheckpoint;

        private readonly object lockObject = new object();

        public EventStoreSubscription(IEventStoreConnection resilientConnection, IJsonSerializer serializer, string streamName, Lazy<long?> lazyLastCheckpoint, Action<long, object> handler, Action<long> persistCheckpoint = null)
        {
            Ensure.NotNull(resilientConnection, nameof(resilientConnection));
            Ensure.NotNullOrWhiteSpace(streamName, nameof(streamName));
            Ensure.NotNull(handler, nameof(handler));
            Ensure.NotNull(serializer, nameof(serializer));

            this.resilientConnection = resilientConnection;
            this.streamName = streamName;
            this.handler = handler;
            this.lazyLastCheckpoint = lazyLastCheckpoint;
            this.serializer = serializer;

            if (persistCheckpoint is null)
                this.shouldPersistCheckpoint = false;
            else
            {
                this.shouldPersistCheckpoint = true;
                this.persistCheckpoint = persistCheckpoint;
            }
        }

        public void Start()
        {
            lock (this.lockObject)
            {
                this.shouldStopNow = false;
                if (this.lastCheckpoint == null)
                    this.lastCheckpoint = this.lazyLastCheckpoint.Value;
            }
            this.log.Info($"Starting subscription of {this.streamName} from" + (!this.lastCheckpoint.HasValue ? " the beginning" : $" checkpoint {this.lastCheckpoint}"));
            this.DoStart();
        }

        public void Stop()
        {
            lock (this.lockObject)
            {
                this.shouldStopNow = true;
                if (this.subscription != null)
                    this.subscription.Stop();
            }
        }

        private void DoStart()
        {
            lock (this.lockObject)
            {
                if (this.subscription != null)
                    this.subscription.Stop();


                this.subscription = this.resilientConnection.SubscribeToStreamFrom(this.streamName, this.lastCheckpoint, CatchUpSubscriptionSettings.Default,
                       (sub, eventAppeared) =>
                       {
                           lock (this.lockObject)
                           {
                               if (!this.shouldStopNow)
                               {
                                   var serialized = Encoding.UTF8.GetString(eventAppeared.Event.Data);
                                   var deserialized = this.serializer.Deserialize(serialized);
                                   this.handler.Invoke(eventAppeared.OriginalEventNumber, deserialized);
                                   this.lastCheckpoint = eventAppeared.OriginalEventNumber;
                                   if (this.shouldPersistCheckpoint)
                                       this.persistCheckpoint(eventAppeared.OriginalEventNumber);
                               }
                           }
                       },
                       sub => this.log.Verbose(
                           $"The subscription of {this.streamName} has caught-up on" + (this.lastCheckpoint.HasValue ? $" checkpoint {this.lastCheckpoint}!" : " the very beginning!")),
                       (sub, reason, ex) =>
                       {
                           if (this.shouldStopNow || reason == SubscriptionDropReason.UserInitiated)
                               return;
                           else if (reason == SubscriptionDropReason.ConnectionClosed || reason == SubscriptionDropReason.CatchUpError)
                           {
                               var seconds = 30;
                               var chkp = this.lastCheckpoint.HasValue ? this.lastCheckpoint : -1;
                               var message = $"The subscription of {this.streamName} stopped because of {reason} on checkpoint {chkp}. Restarting in {seconds} seconds.";
                               if (reason == SubscriptionDropReason.ConnectionClosed)
                                   this.log.Info(message);
                               else
                                   this.log.Error(ex, message);

                               Thread.Sleep(TimeSpan.FromSeconds(seconds));
                               this.log.Info($"Restarting subscription of {this.streamName} on checkpoint {chkp}");
                               this.DoStart();
                               return;
                           }

                           // This should be handled better
                           sub.Stop();
                           throw ex;
                       });
            }
        }
    }
}
