using MercLord.Global.Rendering;
using UnityEditor;
using UnityEngine;

namespace MercLord.Editor
{
    [CustomEditor(typeof(GlobalMapDebugController))]
    public sealed class GlobalMapDebugControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            var controller = (GlobalMapDebugController)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate"))
                {
                    controller.GenerateCurrentSeed();
                    RepaintEditorViews();
                }

                if (GUILayout.Button("Generate New Seed"))
                {
                    controller.GenerateNewSeed();
                    RepaintEditorViews();
                }
            }

            if (GUILayout.Button("Clear Generated"))
            {
                controller.ClearGenerated();
                RepaintEditorViews();
            }
        }

        private static void RepaintEditorViews()
        {
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
