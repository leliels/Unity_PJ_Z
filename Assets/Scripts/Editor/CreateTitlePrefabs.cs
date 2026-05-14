using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BlockPuzzle.EditorTools
{
    public static class CreateTitlePrefabs
    {
        private const string PrefabDir = "Assets/Prefabs/UI/Title";

        [MenuItem("BlockPuzzle/AI 工具/创建 Title Prefab")]
        public static void Create()
        {
            EnsureFolder();
            var font = LoadFont();
            CreateTitlePanel(font);
            CreateSettingsPanel(font);
            AssetDatabase.Refresh();
            Debug.Log("[CreateTitlePrefabs] Title Prefab 已创建在 " + PrefabDir);
        }

        private static Font LoadFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI/Title"))
                AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Title");
        }

        private static void CreateTitlePanel(Font font)
        {
            var root = new GameObject("TitlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.11f, 1f);

            var titleGo = BuildText(root.transform, "TitleText", "快乐消消乐", 72, new Color(1f, 0.9f, 0.45f, 1f), font);
            titleGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 620f);
            titleGo.GetComponent<RectTransform>().sizeDelta = new Vector2(820f, 120f);

            // ModeButtonContainer + 固定的两个模式按钮(美术可直接编辑样式)
            var cGo = new GameObject("ModeButtonContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            cGo.transform.SetParent(root.transform, false);
            cGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 260f);
            cGo.GetComponent<RectTransform>().sizeDelta = new Vector2(540f, 400f);
            var vlg = cGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 32f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;

            // 传统模式按钮
            var tradBtn = BuildButton(cGo.transform, "ModeButton_traditional", "传统模式", new Vector2(520f, 108f), new Color(0.2f, 0.45f, 0.85f, 1f), font);
            // 冒险模式按钮
            var advBtn = BuildButton(cGo.transform, "ModeButton_adventure", "冒险模式", new Vector2(520f, 108f), new Color(0.25f, 0.25f, 0.32f, 1f), font);

            var sGo = BuildButton(root.transform, "SettingsButton", "设置", new Vector2(360f, 88f), new Color(0.35f, 0.35f, 0.45f, 1f), font);
            sGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -250f);

            var mGo = BuildText(root.transform, "MessageText", "", 30, new Color(1f, 0.95f, 0.8f, 1f), font);
            mGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -390f);
            mGo.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 80f);

            var vGo = BuildText(root.transform, "VersionText", "v0.1 M3", 24, new Color(1f, 1f, 1f, 0.55f), font);
            vGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -820f);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/TitlePanel.prefab");
            Object.DestroyImmediate(root);
        }

        private static void CreateSettingsPanel(Font font)
        {
            var root = new GameObject("TitleSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 720f);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.84f);

            BuildText(root.transform, "Title", "设置", 46, Color.white, font)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 280f);

            BuildToggleRow(root.transform, "MusicToggle", "音乐", new Vector2(-30f, 180f), font);
            BuildSlider(root.transform, "MusicSlider", new Vector2(110f, 125f));
            BuildToggleRow(root.transform, "SoundToggle", "音效", new Vector2(-30f, 55f), font);
            BuildSlider(root.transform, "SoundSlider", new Vector2(110f, 0f));
            BuildToggleRow(root.transform, "VibrationToggle", "震动", new Vector2(-30f, -75f), font);

            BuildButton(root.transform, "ClearDataButton", "清除用户数据", new Vector2(420f, 80f), new Color(0.72f, 0.25f, 0.25f, 1f), font)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -220f);
            BuildButton(root.transform, "CloseButton", "关闭", new Vector2(300f, 76f), new Color(0.35f, 0.35f, 0.45f, 1f), font)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -320f);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/TitleSettingsPanel.prefab");
            Object.DestroyImmediate(root);
        }

        private static GameObject BuildText(Transform parent, string name, string text, int size, Color color, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 80f);
            var t = go.GetComponent<Text>();
            t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.font = font;
            return go;
        }

        private static GameObject BuildButton(Transform parent, string name, string label, Vector2 size, Color color, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            go.GetComponent<Button>().targetGraphic = img;
            go.AddComponent<BlockPuzzle.Audio.UIAudioTrigger>();
            var tGo = BuildText(go.transform, "Text", label, 30, Color.white, font);
            var tRt = tGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
            return go;
        }

        private static void BuildToggleRow(Transform parent, string name, string label, Vector2 pos, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().anchoredPosition = pos;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 60f);
            BuildText(go.transform, "Label", label, 30, Color.white, font);
        }

        private static void BuildSlider(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().anchoredPosition = pos;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 40f);
        }
    }
}
