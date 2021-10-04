using System.Linq;

namespace Infrastructure.Messaging
{
    public class ValidationResult
    {
        public ValidationResult(bool isValid, params string[] messages)
        {
            this.IsValid = isValid;
            this.Messages = messages;
        }

        public bool IsValid { get; private set; }
        public string[] Messages { get; private set; }

        public static ValidationResult Ok() => new ValidationResult(true);

        public void Invalidate(string message)
        {
            if (IsValid)
                this.IsValid = false;

            this.Messages = this.Messages.Append(message).ToArray();
        }
    }
}
