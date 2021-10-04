using Infrastructure.EventSourcing;
using Infrastructure.Logging;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Processing;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.RelationalDbSync
{
    public abstract class TableSynchronizer<TEventSourced, TTableRow>
        : ITableSynchronizer
        where TTableRow : ValueObject<TTableRow>, ITableRow
        where TEventSourced : class, IEventSourcedRow<TTableRow>
    {
        protected ILogLite log = LogManager.GetLoggerFor<TEventSourced>();
        private readonly string upsertPollingDesc = typeof(TEventSourced).Name + "-UpsertPolling";
        //private readonly string deletePollingDesc = typeof(TEventSourced).Name + "-DeletePolling";

        protected readonly Type insertedEventType;
        protected readonly Type deletedEventType;
        protected readonly Type restoredEventType;
        protected readonly Type updatedEventType;

        private readonly IEventSourcedRepository repo;
        private TimeSpan idleInterval;
        private TimeSpan pageIntervalSec;
        private TimeSpan onErrorInterval;
        private int pageSize;
        private RunningProcessState state = new RunningProcessState();
        private readonly object lockObject = new object();

        private readonly MessageMetadata metadata;


        public TableSynchronizer(IEventSourcedRepository eventSourcedRepository)
            : this(eventSourcedRepository, "TableSynchronizer", "TableSynchronizer", "localhost", "InProcess")
        { }

        public TableSynchronizer(IEventSourcedRepository eventSourcedRepository,
            string authorId, string authorName, string clientIpAddress, string userAgent)
        {
            this.repo = Ensured.NotNull(eventSourcedRepository, nameof(eventSourcedRepository));

            this.metadata = new MessageMetadata(
                Ensured.NotEmpty(authorId, nameof(authorId)),
                Ensured.NotEmpty(authorName, nameof(authorName)),
                Ensured.NotEmpty(clientIpAddress, nameof(clientIpAddress)),
                Ensured.NotEmpty(userAgent, nameof(userAgent)));

            var es = EventSourcedCreator.New<TEventSourced>();
            this.insertedEventType = es.GetInsertedType();
            this.deletedEventType = es.GetDeletedType();
            this.restoredEventType = es.GetRestoredType();
            this.updatedEventType = es.GetUpdatedType();
        }

        public void Start(CancellationToken token, IDynamicThrottling dynamicThrottling, TimeSpan idleInterval, TimeSpan activeTableInterval, TimeSpan pageIntervalSec, TimeSpan onErrorInterval, int pageSize = 500)
        {
            if (!token.CanBeCanceled) return;

            lock (this.lockObject)
            {
                if (this.state.ShouldBeRunning) return;

                Ensure.NotNull(dynamicThrottling, nameof(dynamicThrottling));

                Ensure.NotNegative(idleInterval.TotalMilliseconds, nameof(idleInterval));
                Ensure.NotNegative(pageIntervalSec.TotalMilliseconds, nameof(pageIntervalSec));
                Ensure.NotNegative(onErrorInterval.TotalMilliseconds, nameof(onErrorInterval));
                Ensure.NotNegative(activeTableInterval.TotalMilliseconds, nameof(activeTableInterval));
                this.idleInterval = idleInterval;
                this.pageIntervalSec = pageIntervalSec;
                this.onErrorInterval = onErrorInterval;

                this.pageSize = Ensured.Positive(pageSize, nameof(pageSize));

                this.state.NotifyStarted();

                var isolatedState = this.state;
                Task.Factory.StartNew(
                    async () =>
                    {
                        while (isolatedState.ShouldBeRunning && !token.IsCancellationRequested)
                        {
                            if (await this.DetectChangesWithResiliency(token, dynamicThrottling))
                                await Task.Delay(activeTableInterval, token);
                            else
                            {
                                await Task.Delay(this.idleInterval, token);
                            }
                        }
                    }, token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            }
        }

        public void Stop()
        {
            lock (this.lockObject)
            {
                if (this.state.ShouldBeRunning)
                {
                    this.state.NotifyCancellation();
                    this.state = new RunningProcessState();
                }
            }
        }

        public abstract string TableName { get; }

        protected virtual void OnInsertDetected(IEventInTransit e) { }
        protected virtual void OnRestoredDetected(IEventInTransit e) { }
        protected virtual void OnUpdatedDetected(IEventInTransit e) { }
        protected virtual void OnDeletedDetected(IEventInTransit e) { }
        protected virtual void OnTableSynced(bool changeDetected) { }

        public abstract Task<TableSlice<TTableRow>> ReadTableAsync(int from, int count);

        private async Task<TableSlice<TTableRow>> ResilientReadTableAsync(int from, int count)
        {
            do
            {
                try
                {
                    return await this.ReadTableAsync(from, count);
                }
                catch (RequestRejectedException ex)
                {
                    this.log.Verbose("The request was rejected. Maybe this is due to configuration. Ignoring error. The first error was: " + ex.Message);
                    await Task.Delay(this.onErrorInterval);
                }
            } while (this.state.ShouldBeRunning);

            throw new TaskCanceledException();
        }

        private async Task<bool> DetectChangesWithResiliency(CancellationToken token, IDynamicThrottling throttling)
        {
            // Start detecting with infinite retries, until canceled or succeeded. 
            // This will make the whole detection system resilient and not crashing because
            // of a process got stuck.
            this.log.Verbose($"Polling changes in table started");
            ICommandInTransit cmd = new PollChangesInRelationalDbTable();
            cmd.SetMetadata(this.metadata);
            var tableRowsKeys = new HashSet<string>();
            var changeDetected = await TaskRetryFactory.Get(
                async () => await this.PollUpserts(token, throttling, cmd, tableRowsKeys) | await this.PollDeletes(token, cmd, tableRowsKeys),
                ex =>
                {
                    //if (ex is RequestRejectedException)
                    //    this.log.Verbose("The request was rejected. Maybe this is due to configuration. Ignoring error. The first error was: " + ex.Message);
                    //else 
                    if (ex is OperationCanceledException)
                        this.log.Verbose("The operation was canceled.");
                    else if (ex is TaskCanceledException)
                        this.log.Verbose("The detection task was cancelled");
                    else
                    {
                        this.log.Error(ex, "An error ocurred on change detection. See exception/s.");
                        this.log.Warning("The change detection will continue.");
                    }
                },
                this.onErrorInterval,
                token);

            if (changeDetected)
                this.log.Success($"Changes detected while polling {tableRowsKeys.Count} rows where saved.");
            else
                this.log.Info($"No changes detected in {tableRowsKeys.Count} rows.");

            this.OnTableSynced(changeDetected);
            return changeDetected;
        }

        private async Task<bool> PollUpserts(CancellationToken token, IDynamicThrottling throttling, ICommandInTransit cmd, HashSet<string> tableRowsKeys)
        {
            var changeDetected = false;
            void markChangeDetected()
            {
                if (!changeDetected)
                    changeDetected = true;
            }

            int sliceStart = 0;
            TableSlice<TTableRow> currentSlice;
            do
            {
                await throttling.WaitUntilAllowedParallelism(token, this.upsertPollingDesc);
                try
                {
                    throttling.NotifyWorkStarted(this.upsertPollingDesc);

                    currentSlice = await this.ResilientReadTableAsync(sliceStart, this.pageSize);
                    this.log.Verbose(currentSlice.Status == TableSliceFetchStatus.Success ? $"Fetched {currentSlice.Rows.Count()} rows. Next row number: {currentSlice.NextRowNumber}"
                        : $"Table or data not found.");

                    throttling.NotifyWorkCompleted(this.upsertPollingDesc);

                    switch (currentSlice.Status)
                    {
                        case TableSliceFetchStatus.Success:
                            sliceStart = currentSlice.NextRowNumber;
                            tableRowsKeys.AddRange(currentSlice.Rows.Select(x => x.Key));

                            this.log.Verbose($"Total rows: {tableRowsKeys.Count()}");

                            foreach (var row in currentSlice.Rows)
                            {
                                if (token.IsCancellationRequested)
                                    throw new OperationCanceledException(token);

                                var eventSourced = await this.repo.TryGetByIdAsync<TEventSourced>(row.Key);
                                if (eventSourced == null)
                                {
                                    eventSourced = EventSourcedCreator.New<TEventSourced>();
                                    // Insert
                                    markChangeDetected();
                                    var e = (IEventInTransit)Activator.CreateInstance(this.insertedEventType, row);
                                    this.OnInsertDetected(e);
                                    eventSourced.Update(cmd, e!);
                                }
                                else
                                {
                                    if (eventSourced.Deleted)
                                    {
                                        // Restored
                                        markChangeDetected();

                                        var e = (IEventInTransit)Activator.CreateInstance(this.restoredEventType, row);
                                        this.OnRestoredDetected(e);
                                        eventSourced.Update(cmd, e);
                                    }
                                    else if (eventSourced.Row != row)
                                    {
                                        // Update
                                        markChangeDetected();

                                        var updatedRows = row.GetDiferentFieldValuesOfAnotherObject(eventSourced.Row);
                                        var e = (IEventInTransit)Activator.CreateInstance(this.updatedEventType, row, updatedRows);
                                        this.OnUpdatedDetected(e);
                                        eventSourced.Update(cmd, e);
                                    }
                                    else
                                    {
                                        // No changes!
                                        continue;
                                    }
                                }

                                await this.PublishChanges(eventSourced);
                            }
                            break;

                        case TableSliceFetchStatus.TableOrDataNotFound:
                        default:
                            // We notify this earlier
                            //throttling.NotifyWorkCompleted(this.upsertPollingDesc);
                            return changeDetected;
                    }

                    // We notify this earlierS
                    //throttling.NotifyWorkCompleted(this.upsertPollingDesc);

                    await Task.Delay(this.pageIntervalSec, token);
                }
                catch (TaskCanceledException)
                {
                    throttling.NotifyWorkCompletedWithError(this.upsertPollingDesc);
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throttling.NotifyWorkCompletedWithError(this.upsertPollingDesc);
                    throw;
                }
                //catch (RequestRejectedException)
                //{
                //    throttling.NotifyWorkCompletedWithError(this.upsertPollingDesc);
                //    throw;
                //}
                catch (Exception ex)
                {
                    throttling.NotifyWorkCompletedWithError(this.upsertPollingDesc);
                    this.log.Error(ex, "Error on polling upserts.");
                    throw;
                }

            } while (!currentSlice.IsEndOfTable);

            return changeDetected;
        }

        private async Task PublishChanges(TEventSourced eventSourced)
        {
            await this.repo.CommitAsync(eventSourced);
        }

        private async Task<bool> PollDeletes(CancellationToken token, ICommandInTransit cmd, HashSet<string> tableRowsKeys)
        {
            var changeDetected = false;
            void markChangeDetected()
            {
                if (!changeDetected)
                    changeDetected = true;
            }

            // We commented out the throttling mechanism because it can take a lot of of hours to complete
            // when running on a slow HD 
            //await throttling.WaitUntilAllowedParallelism(token, this.deletePollingDesc);
            try
            {
                //throttling.NotifyWorkStarted(this.deletePollingDesc);

                await this.repo
                    .GetAsAsyncStream<TEventSourced>()
                    .ForEachAsync(async eventSourced =>
                        {
                            if (token.IsCancellationRequested)
                                throw new OperationCanceledException(token);


                            if (!eventSourced.Deleted && !tableRowsKeys.Contains(eventSourced.Row.Key))
                            {
                                markChangeDetected();

                                var e = (IEventInTransit)Activator.CreateInstance(this.deletedEventType, eventSourced.Row);
                                this.OnDeletedDetected(e);
                                eventSourced.Update(cmd, e);

                                await this.PublishChanges(eventSourced);
                            }
                        });

                //throttling.NotifyWorkCompleted(this.deletePollingDesc);
            }
            //catch (RequestRejectedException)
            //{
            //    throttling.NotifyWorkCompletedWithError(this.deletePollingDesc);
            //    throw;
            //}
            catch (Exception ex)
            {
                //throttling.NotifyWorkCompletedWithError(this.deletePollingDesc);
                this.log.Error(ex, "Error on polling deletes.");
                throw;
            }

            this.log.Verbose("Deletes polling finished successfully. " + (changeDetected ? "Deletes detected." : "No deletes where detected"));
            return changeDetected;
        }

        private class RunningProcessState
        {
            internal RunningProcessState()
            {
                this.ShouldBeRunning = false;
            }

            internal bool ShouldBeRunning { get; set; }

            internal void NotifyCancellation() => this.ShouldBeRunning = false;
            internal void NotifyStarted() => this.ShouldBeRunning = true;
        }
    }
}
