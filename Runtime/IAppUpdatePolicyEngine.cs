namespace BizSim.Google.Play.AppUpdate
{
    public interface IAppUpdatePolicyEngine
    {
        PolicyDecision Evaluate(AppUpdatePolicyContext context);
    }
}
