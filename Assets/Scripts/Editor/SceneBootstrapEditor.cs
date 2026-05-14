using BlockPuzzle.Core;
using UnityEditor;

namespace BlockPuzzle.Editor
{
    /// <summary>
    /// SceneBootstrap 专用 Inspector。
    /// 当前仅用默认绘制;所有数值配置已收束到 GameConfig。
    /// </summary>
    [CustomEditor(typeof(SceneBootstrap))]
    public sealed class SceneBootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
