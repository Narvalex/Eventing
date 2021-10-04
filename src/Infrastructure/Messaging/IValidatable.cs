namespace Infrastructure.Messaging
{
    public interface IValidatable
    {
        ValidationResult ExecuteBasicValidation();
    }
}
