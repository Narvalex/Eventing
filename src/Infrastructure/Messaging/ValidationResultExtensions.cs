using System;

namespace Infrastructure.Messaging
{
    public static class ValidationResultExtensions
    {
        public static ValidationResult And(this ValidationResult result, bool requirement, string message)
        {
            if (!requirement)
                result.Invalidate(message);
            return result;
        }

        public static ValidationResult AndRequires(this ValidationResult result, bool requirement, string message) => 
            And(result, requirement, message);

        public static ValidationResult AndIsInvalidIf(this ValidationResult result, bool invalidState, string message) =>
           And(result, !invalidState, message);

        public static ValidationResult Or(this ValidationResult result, bool invalidState, string message) => 
            And(result, !invalidState, message);
    }
}
