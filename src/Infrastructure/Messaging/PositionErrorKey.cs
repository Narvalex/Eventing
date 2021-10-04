using Infrastructure.Utils;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public class PositionErrorKey
    {
        private static string[] keys = Ensured.AllUniqueConstStrings<PositionErrorKey>().ToArray();

        public static bool IsValidPositionError(string? error) => error is null || keys.Any(x => x == error);

        public const string NotSupported = "NOT_SUPPORTED";
        public const string NotYetResolved = "NOT_YET_RESOLVED";
        public const string PermissionDenied = "PERMISSION_DENIED";
        public const string PositionUnavailable = "POSITION_UNAVAILABLE";
        public const string Timeout = "TIMEOUT";
        public const string UnknownError = "UNKNOWN_ERROR";
    }
}
