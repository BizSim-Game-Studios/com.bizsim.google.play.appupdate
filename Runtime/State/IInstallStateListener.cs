using System;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Listener for continuous install state updates during a flexible flow. The controller
    /// subscribes once at Awake and marshals the event to consumer code.
    /// </summary>
    public interface IInstallStateListener
    {
        event Action<InstallState> OnStateUpdate;
        void StartListening();
        void StopListening();
    }
}
