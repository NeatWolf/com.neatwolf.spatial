using NeatWolf.Spatial.Analysis;
using UnityEditor;
using UnityEngine;

namespace NeatWolf.Spatial.Editor
{
    [CustomEditor(typeof(SceneAnalyzer))]
    public class SceneAnalyzerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var myScript = (SceneAnalyzer)target;

            var runningAnalysis = myScript.IsAnalysisRunning();

            EditorGUI.BeginDisabledGroup(runningAnalysis);
            //if (!runningAnalysis)
            {
                DrawDefaultInspector();
                if (myScript.BoxColliderBounds == null)
                    EditorGUILayout.HelpBox("Please set the analysis bounds before starting the analysis.",
                        MessageType.Error);

                if (GUILayout.Button("Analyze Scene")) myScript.StartAnalyzeScene();
            }

            EditorGUI.EndDisabledGroup();


            EditorGUI.BeginDisabledGroup(!runningAnalysis);
            if (GUILayout.Button("Force Stop")) myScript.StopAnalysis();
            EditorGUI.EndDisabledGroup();
            if (runningAnalysis)
                EditorGUILayout.HelpBox("Scene analysis is currently running...", MessageType.Info);
        }
    }
}