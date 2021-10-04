namespace Infrastructure.Messaging
{
    public abstract class Message : IMessage
    {
        // It is protected to avoid being serialized when storing
        protected IMessageMetadata metadata;

        public IMessageMetadata GetMessageMetadata() 
            => this.metadata;

        protected internal void SetMetadata(IMessageMetadata metadata)
            => this.metadata = metadata;

        protected virtual ValidationResult OnExecutingBasicValidation() => this.IsValid();

        ValidationResult IValidatable.ExecuteBasicValidation() => this.OnExecutingBasicValidation();
    }
}
