using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    [CustomEditor(typeof(Dialogue))]
    class DialogueInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Dialogue selected = target as Dialogue;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Dialogue Nodes:");
                GUILayout.Label(selected.GetAllNodes().Length.ToString());
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Dialogue Options:");
                GUILayout.Label(selected.GetAllOptions().Length.ToString());
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Nodes count (editor info):");
                GUILayout.Label(selected.EditorInfo.Nodes.ToString());
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("NodesIndexes count (editor info):");
                GUILayout.Label(selected.EditorInfo.NodesIndexes.Count.ToString());
            }
            GUILayout.EndHorizontal();

        }
    }
}
