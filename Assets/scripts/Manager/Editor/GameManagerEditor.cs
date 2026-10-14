using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        GameManager gameManager = (GameManager)target;
        if (GUILayout.Button("完成当前关卡", GUILayout.Height(30f)))
        {
            gameManager.DebugCompleteLevel();
        }
    }
}
