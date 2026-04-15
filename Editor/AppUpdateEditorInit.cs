using UnityEditor;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    /// <summary>
    /// Auto-registers the <c>BIZSIM_APPUPDATE_INSTALLED</c> scripting define at editor load, so
    /// consumer shared code can use <c>#if BIZSIM_APPUPDATE_INSTALLED</c> guards without manual
    /// Player Settings edits. Runs once per editor session via <see cref="InitializeOnLoadAttribute"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class AppUpdateEditorInit
    {
        static AppUpdateEditorInit()
        {
            BizSimDefineManager.AddDefine("BIZSIM_APPUPDATE_INSTALLED",
                BizSimDefineManager.GetRelevantPlatforms());
        }
    }
}
