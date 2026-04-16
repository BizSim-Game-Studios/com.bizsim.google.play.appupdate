using System;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Abstraction over <see cref="DateTime.UtcNow"/> for testability. Injected into
    /// <see cref="AppUpdatePolicyEngine"/> and staleness-math helpers so tests can control time
    /// without relying on real clock progression.
    /// </summary>
    /// <remarks>
    /// S10 security: this interface is <c>internal</c> — consumers cannot supply a clock that
    /// manipulates staleness calculations. Test assemblies access it via
    /// <c>[assembly: InternalsVisibleTo]</c> in <c>AssemblyInfo.cs</c>.
    /// </remarks>
    internal interface IClock
    {
        DateTime UtcNow { get; }
    }
}
