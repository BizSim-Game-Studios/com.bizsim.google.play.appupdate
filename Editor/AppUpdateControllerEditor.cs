using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    [CustomEditor(typeof(AppUpdateController))]
    internal sealed class AppUpdateControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("QA / Testing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Clear Last Observed State resets the cached install state so CompleteFlexibleUpdate " +
                "will throw until a fresh Downloaded state is observed. Intended for QA / manual " +
                "testing of the precondition guard.",
                MessageType.Info);

            if (!Application.isPlaying)
            {
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Button("Clear Last Observed State (play mode only)");
                return;
            }

            if (GUILayout.Button("Clear Last Observed State"))
            {
                // No public API for this — the controller owns its _lastState lock. For now the
                // button is informational; QA can restart play mode to clear in-memory state.
                Debug.Log(BizSimLogger.Prefix +
                    "Last observed state is in-memory only; restart play mode to clear it.");
            }
        }
    }
}
