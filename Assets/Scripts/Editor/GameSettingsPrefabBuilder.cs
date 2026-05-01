using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// 一键创建 GameFlowUI 所需的三个 Prefab：
    /// BackToTitleButton、GameSettingsButton、GameSettingsPanel
    /// 放入 Assets/Resources/Prefabs/UI/
    /// </summary>
    public static class GameSettingsPrefabBuilder
    {
        private const string OutputFolder = "Assets/Resources/Prefabs/UI";

        [MenuItem("BlockPuzzle/AI 工具/Build Game Settings Prefabs")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder("Assets/Resources/Prefabs/UI");

            BuildBackToTitleButton();
            BuildGameSettingsButton();
            BuildGameSettingsPanel();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameSettingsPrefabBuilder] 3 个 Prefab 创建/更新完成");
        }

        // ==================== BackToTitleButton ====================
        private static void BuildBackToTitleButton()
        {
            var go = new GameObject("BackToTitleButton");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 100f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(80f, -80f);

            var image = go.AddComponent<Image>();
            var btnBackSprite = LoadSprite("Assets/Resources/Art/UI/Buttons/btn_back.png", "btn_back_0");
            image.sprite = btnBackSprite;
            image.color = Color.white;
            image.preserveAspect = true;
            if (btnBackSprite != null)
                image.SetNativeSize();

            go.AddComponent<Button>().targetGraphic = image;

            SavePrefab(go, OutputFolder + "/BackToTitleButton.prefab");
            Object.DestroyImmediate(go);
        }

        // ==================== GameSettingsButton ====================
        private static void BuildGameSettingsButton()
        {
            var go = new GameObject("GameSettingsButton");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 100f);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-80f, -80f);

            var image = go.AddComponent<Image>();
            var btnSettingsSprite = LoadSprite("Assets/Resources/Art/UI/Buttons/btn_settings.png", "btn_settings_0");
            image.sprite = btnSettingsSprite;
            image.color = Color.white;
            image.preserveAspect = true;
            if (btnSettingsSprite != null)
                image.SetNativeSize();

            go.AddComponent<Button>().targetGraphic = image;

            SavePrefab(go, OutputFolder + "/GameSettingsButton.prefab");
            Object.DestroyImmediate(go);
        }

        // ==================== GameSettingsPanel ====================
        private static void BuildGameSettingsPanel()
        {
            // 加载素材
            var btnPrimary = LoadSprite("Assets/Resources/Art/UI/Buttons/btn_primary.png", "btn_primary_0");
            var icoMusic = LoadSprite("Assets/Resources/Art/UI/Icons/ico_music.png", "ico_music_0");
            var icoSound = LoadSprite("Assets/Resources/Art/UI/Icons/ico_sound.png", "ico_sound_0");
            var icoVibration = LoadSprite("Assets/Resources/Art/UI/Icons/ico_vibration.png", "ico_vibration_0");
            var btnToggleOff = LoadSprite("Assets/Resources/Art/UI/Buttons/btn_toggle_off.png", "btn_toggle_off_0");
            var btnToggleOn = LoadSprite("Assets/Resources/Art/UI/Buttons/btn_toggle_on.png", "btn_toggle_on_0");

            // 面板根
            var panel = new GameObject("GameSettingsPanel");
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(720f, 820f);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.06f, 0.10f, 0.94f);

            // 标题
            CreateLabel(panel.transform, "Title", "设置", 44, new Vector2(0f, 340f));

            // --- 音乐行 ---
            var musicRow = CreateSettingRow(panel.transform, "MusicRow", new Vector2(0f, 210f), btnPrimary, icoMusic, "音乐");
            CreateToggleInRow(musicRow.transform, "MusicToggle", new Vector2(240f, 0f));
            CreateSliderInRow(musicRow.transform, "MusicSlider", new Vector2(130f, -55f), 280f);

            // --- 音效行 ---
            var soundRow = CreateSettingRow(panel.transform, "SoundRow", new Vector2(0f, 60f), btnPrimary, icoSound, "音效");
            CreateToggleInRow(soundRow.transform, "SoundToggle", new Vector2(240f, 0f));
            CreateSliderInRow(soundRow.transform, "SoundSlider", new Vector2(130f, -55f), 280f);

            // --- 震动行 ---
            var vibrationRow = CreateSettingRow(panel.transform, "VibrationRow", new Vector2(0f, -90f), btnPrimary, icoVibration, "震动");
            // 震动使用图片按钮开关
            var vibBtnGo = new GameObject("VibrationToggleBtn");
            vibBtnGo.transform.SetParent(vibrationRow.transform, false);
            var vibBtnRect = vibBtnGo.AddComponent<RectTransform>();
            vibBtnRect.sizeDelta = new Vector2(156f, 75f);
            vibBtnRect.anchoredPosition = new Vector2(220f, 0f);
            var vibBtnImage = vibBtnGo.AddComponent<Image>();
            vibBtnImage.sprite = btnToggleOff; // 默认 off
            vibBtnImage.preserveAspect = true;
            if (btnToggleOff != null)
                vibBtnImage.SetNativeSize();
            vibBtnGo.AddComponent<Button>().targetGraphic = vibBtnImage;

            // --- 功能按钮 ---
            CreateActionButton(panel.transform, "RestartButton", "重新开始", new Vector2(0f, -210f), new Color(0.22f, 0.48f, 0.85f, 1f));
            CreateActionButton(panel.transform, "ReturnTitleButton", "返回标题", new Vector2(0f, -300f), new Color(0.35f, 0.35f, 0.45f, 1f));
            CreateActionButton(panel.transform, "ContinueButton", "继续游戏", new Vector2(0f, -390f), new Color(0.35f, 0.55f, 0.35f, 1f));

            SavePrefab(panel, OutputFolder + "/GameSettingsPanel.prefab");
            Object.DestroyImmediate(panel);
        }

        // ==================== 辅助方法 ====================

        private static GameObject CreateSettingRow(Transform parent, string name, Vector2 position, Sprite bgSprite, Sprite icon, string label)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(620f, 80f);
            rowRect.anchoredPosition = position;

            // btn_primary 作为名称背景
            var bgGo = new GameObject("LabelBg");
            bgGo.transform.SetParent(row.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(196f, 92f);
            bgRect.anchoredPosition = new Vector2(-160f, 0f);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.color = Color.white;
            bgImage.preserveAspect = true;
            if (bgSprite != null)
                bgImage.SetNativeSize();

            // 设置项名称文字（放在 bg 上）
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(bgGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(48f, 0f);
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = label;
            labelText.fontSize = 28;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (labelText.font == null)
                labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 图标
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(bgGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(48f, 48f);
            iconRect.anchoredPosition = new Vector2(-72f, 0f);
            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;

            return row;
        }

        private static void CreateToggleInRow(Transform parent, string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80f, 42f);
            rect.anchoredPosition = position;

            var toggle = go.AddComponent<Toggle>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var check = new GameObject("Checkmark");
            check.transform.SetParent(bg.transform, false);
            var checkRect = check.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(4f, 4f);
            checkRect.offsetMax = new Vector2(-4f, -4f);
            var checkImage = check.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.9f, 0.45f, 1f);

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;
        }

        private static void CreateSliderInRow(Transform parent, string name, Vector2 position, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 24f);
            rect.anchoredPosition = position;

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.65f, 1f, 1f);

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
        }

        private static void CreateActionButton(Transform parent, string name, string text, Vector2 position, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 72f);
            rect.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            image.color = color;
            go.AddComponent<Button>().targetGraphic = image;

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = text;
            labelText.fontSize = 32;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (labelText.font == null)
                labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void CreateLabel(Transform parent, string name, string text, int fontSize, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 60f);
            rect.anchoredPosition = position;
            var label = go.AddComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Sprite LoadSprite(string assetPath, string spriteName)
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in objects)
            {
                if (obj is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }
            // fallback: try load main asset
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void SavePrefab(GameObject go, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
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
