using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BlockPuzzle.Core;

namespace BlockPuzzle.Editor
{
    /// <summary>
    /// 一键将 ScorePopup.prefab 绑定到 Boot 场景中 SceneBootstrap 的 _scorePopupPrefab 字段。
    /// </summary>
    public static class BindScorePopupPrefab
    {
        [MenuItem("BlockPuzzle/AI 工具/绑定 ScorePopup Prefab 到场景")]
        public static void Execute()
        {
            // 确保 Boot 场景已打开
            const string bootScenePath = "Assets/Scenes/Boot.unity";
            var scene = EditorSceneManager.OpenScene(bootScenePath, OpenSceneMode.Single);
            if (!scene.isLoaded)
            {
                Debug.LogError("[BindScorePopupPrefab] 无法打开 Boot 场景: " + bootScenePath);
                return;
            }

            var bs = Object.FindFirstObjectByType<SceneBootstrap>();
            if (bs == null)
            {
                // 尝试在所有根对象中搜索
                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    bs = rootGo.GetComponent<SceneBootstrap>();
                    if (bs != null) break;
                }
            }
            if (bs == null)
            {
                Debug.LogError("[BindScorePopupPrefab] Boot 场景中未找到 SceneBootstrap 组件");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ScorePopup.prefab");
            if (prefab == null)
            {
                Debug.LogError("[BindScorePopupPrefab] Assets/Prefabs/UI/ScorePopup.prefab 未找到");
                return;
            }

            var so = new SerializedObject(bs);
            var prop = so.FindProperty("_scorePopupPrefab");
            if (prop == null)
            {
                Debug.LogError("[BindScorePopupPrefab] _scorePopupPrefab 字段未找到");
                return;
            }

            prop.objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(bs.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[BindScorePopupPrefab] 已成功绑定 ScorePopup.prefab 并保存场景");
        }
    }
}
