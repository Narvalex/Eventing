using Infrastructure.Messaging;

namespace Erp.Domain.Tests.Helpers
{
    public static class CommandExtensions
    {
        public static ICommandInTest WithCorrelationId(this ICommandInTest cmd, string correlationId)
        {
            cmd.SetCorrelationId(correlationId);
            return cmd;
        }
    }
}
