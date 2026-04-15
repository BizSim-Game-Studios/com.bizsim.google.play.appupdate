using System;

namespace BizSim.Google.Play.AppUpdate
{
    public readonly struct AppUpdateError : IEquatable<AppUpdateError>
    {
        public readonly InstallErrorCode Code;
        public readonly string Message;
        public readonly bool Retryable;
        public readonly DateTime OccurredAtUtc;

        public AppUpdateError(InstallErrorCode code, string message, bool retryable, DateTime occurredAtUtc)
        {
            Code = code;
            Message = message ?? "";
            Retryable = retryable;
            OccurredAtUtc = occurredAtUtc;
        }

        public static bool IsRetryable(InstallErrorCode c) =>
            c == InstallErrorCode.ErrorInternalError ||
            c == InstallErrorCode.ErrorUnknown ||
            c == InstallErrorCode.Timeout ||
            c == InstallErrorCode.BridgeNotInitialized;
        // Per CROSS-INVARIANTS §5 retry policy table.

        public bool Equals(AppUpdateError other)
            => Code == other.Code
            && Retryable == other.Retryable
            && OccurredAtUtc == other.OccurredAtUtc
            && string.Equals(Message, other.Message, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is AppUpdateError e && Equals(e);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = (int)Code;
                h = (h * 397) ^ (Retryable ? 1 : 0);
                h = (h * 397) ^ OccurredAtUtc.GetHashCode();
                h = (h * 397) ^ (Message?.GetHashCode() ?? 0);
                return h;
            }
        }

        public static bool operator ==(AppUpdateError a, AppUpdateError b) => a.Equals(b);
        public static bool operator !=(AppUpdateError a, AppUpdateError b) => !a.Equals(b);
    }
}
