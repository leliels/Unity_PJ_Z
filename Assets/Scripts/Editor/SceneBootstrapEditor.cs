using BlockPuzzle.Core;
using UnityEditor;
using UnityEngine;

namespace BlockPuzzle.Editor
{
    /// <summary>
    /// SceneBootstrap 专用 Inspector，将本场景使用的计分配置入口明确暴露出来。
    /// </summary>
    [CustomEditor(typeof(SceneBootstrap))]
    public sealed class SceneBootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("玩法配置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_scoreConfig"),
                new GUIContent(
                    "本场景使用的计分配置",
                    "本场景启动时传给 ScoreManager 的计分配置。可为不同模式指定不同 ScoreConfig；为空时默认加载 Resources/Configs/ScoreConfig。"));

            EditorGUILayout.Space(8f);
            DrawPropertiesExcluding(serializedObject, "m_Script", "_scoreConfig");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
