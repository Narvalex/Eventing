using System;

namespace Infrastructure.Messaging
{
    public static class ValidatableExtensions
    {
        public static ValidationResult IsValid(this IValidatable request)
            => ValidationResult.Ok();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="requirement">If true the requirement is satisface</param>
        /// <param name="message">The message that will be show user requirement is not satisface</param>
        /// <returns></returns>
        public static ValidationResult Requires(this IValidatable request, bool requirement, string message)
        {
            var validation = ValidationResult.Ok();
            if (!requirement)
                validation.Invalidate(message);
            return validation;
        }

        public static ValidationResult IsInvalidIf(this IValidatable request, bool invalidState, string message)
        {
            return Requires(request, !invalidState, message);
        }
    }
}
