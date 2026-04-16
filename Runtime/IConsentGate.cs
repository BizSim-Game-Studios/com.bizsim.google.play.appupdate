namespace BizSim.Google.Play.AppUpdate
{
    public interface IConsentGate
    {
        bool IsConsented(AppUpdatePolicyContext context);
    }
}
