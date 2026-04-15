namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Exception type surfaced from AppUpdateController's async API when a flow fails. Wraps
    /// <see cref="AppUpdateError"/> so consumers can inspect the error code + retryable flag.
    /// </summary>
    public sealed class AppUpdateException : System.Exception
    {
        public AppUpdateError Error { get; }
        public AppUpdateException(AppUpdateError error)
            : base($"{error.Code}: {error.Message}") => Error = error;
    }
}
