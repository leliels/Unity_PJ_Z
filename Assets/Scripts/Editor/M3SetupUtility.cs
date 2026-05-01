#if UNITY_EDITOR
using System.Collections.Generic;
using BlockPuzzle.Audio;
using BlockPuzzle.Mode;
using BlockPuzzle.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// M3 框架资源初始化工具：创建 Title 场景、模式配置和占位音效配置。
    /// </summary>
    public static class M3SetupUtility
    {
        [MenuItem("BlockPuzzle/AI 工具/M3 Setup Runtime Assets")]
        public static void RunSetup()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Configs");
            EnsureFolder("Assets/Resources/Audio");
            EnsureFolder("Assets/Resources/Audio/SFX");
            EnsureFolder("Assets/Resources/Audio/BGM");
            EnsureFolder("Assets/Scenes");

            AssetDatabase.ImportAsset("Assets/Resources/Audio/SFX/snd_placeholder_click.wav", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Resources/Audio/BGM/bgm_placeholder_loop.wav", ImportAssetOptions.ForceUpdate);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/snd_placeholder_click.wav");
            AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/BGM/bgm_placeholder_loop.wav");

            AudioCue cue = LoadOrCreate<AudioCue>("Assets/Resources/Configs/PlaceholderAudioCue.asset");
            cue.EditorSetValues(new[] { clip }, 0.85f, 0f, new Vector2(0.95f, 1.05f), false, 0.03f, true);
            EditorUtility.SetDirty(cue);

            AudioLibrary library = LoadOrCreate<AudioLibrary>("Assets/Resources/Configs/AudioLibrary.asset");
            library.EditorSetAll(cue);
            library.EditorSetBgm(bgm, bgm);
            EditorUtility.SetDirty(library);

            GameModeConfig traditional = LoadOrCreate<GameModeConfig>("Assets/Resources/Configs/Mode_Traditional.asset");
            traditional.EditorSetValues(GameModeConfig.TraditionalId, "传统模式", true, false, "Boot", 0, "进入当前已实现玩法");
            EditorUtility.SetDirty(traditional);

            GameModeConfig adventure = LoadOrCreate<GameModeConfig>("Assets/Resources/Configs/Mode_Adventure.asset");
            adventure.EditorSetValues(GameModeConfig.AdventureId, "冒险模式", true, true, string.Empty, 10, "冒险模式敬请期待");
            EditorUtility.SetDirty(adventure);

            ModeCatalog catalog = LoadOrCreate<ModeCatalog>("Assets/Resources/Configs/ModeCatalog.asset");
            catalog.EditorSetModes(new[] { traditional, adventure });
            EditorUtility.SetDirty(catalog);

            CreateTitleScene("Assets/Scenes/Title.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M3Setup] Runtime assets created/updated.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void CreateTitleScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.07f, 0.11f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 11f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.AddComponent<AudioListener>();

            var titleGo = new GameObject("TitleCanvas");
            titleGo.AddComponent<TitleUI>();
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
