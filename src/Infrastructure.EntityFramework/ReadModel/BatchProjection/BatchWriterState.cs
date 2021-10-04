namespace Infrastructure.EntityFramework.ReadModel
{
    public enum BatchWriterState
    {
        BatchWriteStopped,
        BatchWriteIsRunning,
        BatchWriteIsCanceled,
        BatchDbContextSaveChangesHasFaulted,
        LocalDbContextChangesHasFaulted,
        DirectWriteIsEnabled
    }
}
