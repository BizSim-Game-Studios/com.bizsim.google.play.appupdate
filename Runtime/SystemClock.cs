using System;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Production clock that delegates to <see cref="DateTime.UtcNow"/>. Used as the default
    /// <see cref="IClock"/> implementation throughout the package.
    /// </summary>
    /// <remarks>
    /// S10 security: this class is <c>internal</c> — see <see cref="IClock"/> remarks.
    /// </remarks>
    internal sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new();
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
