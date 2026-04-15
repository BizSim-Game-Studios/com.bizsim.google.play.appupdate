using UnityEngine;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Samples.BasicIntegration
{
    /// <summary>
    /// Demonstrates a "smart" update policy that uses <see cref="AppUpdateInfo"/>
    /// classifier helpers to decide between immediate, flexible, and no prompt.
    ///
    /// Policy:
    /// - If priority &gt;= 4 and immediate flow is allowed, run the blocking immediate flow.
    /// - Else if priority &gt;= 2 AND staleness &gt;= 7 days and flexible flow is allowed, run the flexible flow.
    /// - Otherwise, stay silent this session.
    /// </summary>
    public class SmartUpdatePolicySample : MonoBehaviour
    {
        [SerializeField] private int _immediateMinPriority = 4;
        [SerializeField] private int _flexibleMinPriority = 2;
        [SerializeField] private int _flexibleMinStalenessDays = 7;

        private async void Start()
        {
            AppUpdateInfo info;
            try
            {
                info = await AppUpdateController.Instance.CheckForUpdateAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SmartUpdatePolicySample] CheckForUpdate failed: {ex.Message}");
                return;
            }

            if (!info.IsUpdateAvailable)
            {
                Debug.Log("[SmartUpdatePolicySample] No update available.");
                return;
            }

            if (info.IsImmediateUpdateRequired(minPriority: _immediateMinPriority))
            {
                Debug.Log($"[SmartUpdatePolicySample] Priority {info.UpdatePriority} — launching immediate flow.");
                var err = await AppUpdateController.Instance.StartImmediateUpdateAsync();
                if (err.HasValue)
                    Debug.LogWarning($"[SmartUpdatePolicySample] Immediate flow failed: {err.Value.Code}");
            }
            else if (info.IsFlexibleUpdateRecommended(
                         minPriority: _flexibleMinPriority,
                         minStalenessDays: _flexibleMinStalenessDays))
            {
                Debug.Log($"[SmartUpdatePolicySample] Priority {info.UpdatePriority}, staleness {info.ClientVersionStalenessDays}d — launching flexible flow.");
                var err = await AppUpdateController.Instance.StartFlexibleUpdateAsync();
                if (err.HasValue)
                    Debug.LogWarning($"[SmartUpdatePolicySample] Flexible flow failed: {err.Value.Code}");
            }
            else
            {
                Debug.Log("[SmartUpdatePolicySample] Update available but below policy thresholds — staying silent this session.");
            }
        }
    }
}
