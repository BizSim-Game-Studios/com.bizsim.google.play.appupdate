using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    [CustomEditor(typeof(AppUpdateMockConfig))]
    internal sealed class AppUpdateMockConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _availability;
        private SerializedProperty _availableVersion;
        private SerializedProperty _priority;
        private SerializedProperty _stalenessDays;
        private SerializedProperty _allowFlexible;
        private SerializedProperty _allowImmediate;
        private SerializedProperty _flexibleDuration;
        private SerializedProperty _immediateDuration;
        private SerializedProperty _errorCode;
        private SerializedProperty _failureAt;

        private void OnEnable()
        {
            _availability      = serializedObject.FindProperty("SimulatedAvailability");
            _availableVersion  = serializedObject.FindProperty("SimulatedAvailableVersionCode");
            _priority          = serializedObject.FindProperty("SimulatedUpdatePriority");
            _stalenessDays     = serializedObject.FindProperty("SimulatedClientVersionStalenessDays");
            _allowFlexible     = serializedObject.FindProperty("AllowFlexible");
            _allowImmediate    = serializedObject.FindProperty("AllowImmediate");
            _flexibleDuration  = serializedObject.FindProperty("FlexibleDownloadDurationSeconds");
            _immediateDuration = serializedObject.FindProperty("ImmediateFlowDurationSeconds");
            _errorCode         = serializedObject.FindProperty("SimulatedErrorCode");
            _failureAt         = serializedObject.FindProperty("SimulatedFailureAt");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Availability", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_availability);
            EditorGUILayout.PropertyField(_availableVersion);
            EditorGUILayout.PropertyField(_stalenessDays);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Priority & allowed flows", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_priority);
            EditorGUILayout.PropertyField(_allowFlexible);
            EditorGUILayout.PropertyField(_allowImmediate);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Flow durations (seconds)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_flexibleDuration);
            EditorGUILayout.PropertyField(_immediateDuration);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Error injection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_errorCode);
            EditorGUILayout.PropertyField(_failureAt);
            EditorGUILayout.HelpBox(
                "Set SimulatedErrorCode to a non-NoError value to force the flow to fail. " +
                "SimulatedFailureAt controls which install status the flexible state machine " +
                "stops at before firing the error (Unknown = don't inject a stop).",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
