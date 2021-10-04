namespace Infrastructure.IdGeneration
{
    /// <summary>
    /// Generates random alphnumerical strings.
    /// </summary>
    public interface IUniqueIdGenerator
    {
        /// <summary>
        /// Generates a random token string.
        /// </summary>
        /// <returns>A new unique string</returns>
        string New();
    }
}
