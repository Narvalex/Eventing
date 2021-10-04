namespace Infrastructure.Messaging
{
    /// <summary>
    /// Representa a un comando enviado por parte de un saga para que se ejecute 
    /// en el event handler de un aggregate que expone como su api al evento comando.
    /// </summary>
    public abstract class PersistentCommand : Event
    {
        /// <summary>
        /// Crea una instancia de un evento comando.
        /// </summary>
        /// <param name="correlationId">El id de correlación corresponde al id del saga 
        /// que emitió el comando, como un evento. Es el mismo que el id del stream, por que pertence 
        /// al stream del saga.</param>
        public PersistentCommand(string correlationId)
        {
            this.CorrelationId = correlationId;
        }

        public override string StreamId => this.CorrelationId;
        public string CorrelationId { get; }
    }
}
