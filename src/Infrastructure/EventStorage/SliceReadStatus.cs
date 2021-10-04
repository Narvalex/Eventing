namespace Infrastructure.EventStorage
{
    public enum SliceFetchStatus
    {
        /// <summary>
        /// The read was successful.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The stream was not found.
        /// </summary>
        StreamNotFound = 1,
    }
}
