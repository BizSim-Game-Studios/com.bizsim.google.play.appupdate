namespace BizSim.Google.Play.AppUpdate
{
    public sealed class AlwaysAllowConsentGate : IConsentGate
    {
        public bool IsConsented(AppUpdatePolicyContext context) => true;
    }
}
