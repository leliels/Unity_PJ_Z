using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BlockPuzzle.Editor
{
    /// <summary>
    /// 一键生成消除飘字相关 Prefab（ComboFloatingScore + ScorePopup）。
    /// 生成后可在 Inspector 中自由调整字体、颜色、布局等视觉参数。
    /// </summary>
    public static class CreateScorePopupPrefabs
    {
        [MenuItem("BlockPuzzle/AI 工具/创建飘字 Prefab（ScorePopup + Combo）")]
        public static void Execute()
        {
            CreateComboFloatingScorePrefab();
            CreateScorePopupPrefab();
            AssetDatabase.Refresh();
            Debug.Log("[CreateScorePopupPrefabs] 完成创建 ComboFloatingScore.prefab 和 ScorePopup.prefab");
        }

        private static void CreateComboFloatingScorePrefab()
        {
            var root = new GameObject("ComboFloatingScore");
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 80);

            // ComboLabel
            var labelGo = new GameObject("ComboLabel");
            labelGo.transform.SetParent(root.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(-60, 0);
            labelRect.sizeDelta = new Vector2(160, 70);
            var labelTxt = labelGo.AddComponent<Text>();
            labelTxt.text = "Combo \u00d7";
            labelTxt.fontSize = 42;
            labelTxt.alignment = TextAnchor.MiddleCenter;
            labelTxt.color = new Color(1f, 0.75f, 0.2f, 1f);
            labelTxt.font = GetFont();
            labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelTxt.verticalOverflow = VerticalWrapMode.Overflow;
            var labelOL = labelGo.AddComponent<Outline>();
            labelOL.effectColor = new Color(0, 0, 0, 0.7f);
            labelOL.effectDistance = new Vector2(2, -2);

            // MultiplierText
            var mulGo = new GameObject("MultiplierText");
            mulGo.transform.SetParent(root.transform, false);
            var mulRect = mulGo.AddComponent<RectTransform>();
            mulRect.anchoredPosition = new Vector2(80, 0);
            mulRect.sizeDelta = new Vector2(60, 70);
            var mulTxt = mulGo.AddComponent<Text>();
            mulTxt.text = "2";
            mulTxt.fontSize = 52;
            mulTxt.alignment = TextAnchor.MiddleCenter;
            mulTxt.color = new Color(1f, 0.9f, 0.3f, 1f);
            mulTxt.font = GetFont();
            mulTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            mulTxt.verticalOverflow = VerticalWrapMode.Overflow;
            var mulOL = mulGo.AddComponent<Outline>();
            mulOL.effectColor = new Color(0, 0, 0, 0.7f);
            mulOL.effectDistance = new Vector2(2, -2);

            // 挂载 ComboFloatingScoreView
            var cv = root.AddComponent<UI.ComboFloatingScoreView>();
            var so = new SerializedObject(cv);
            so.FindProperty("_comboLabelText").objectReferenceValue = labelTxt;
            so.FindProperty("_multiplierText").objectReferenceValue = mulTxt;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = "Assets/Prefabs/UI/ComboFloatingScore.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[CreateScorePopupPrefabs] Created: {path}");
        }

        private static void CreateScorePopupPrefab()
        {
            var root = new GameObject("ScorePopup");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(960, 220);
            root.AddComponent<CanvasGroup>();

            // ComboModule (实例化已保存的 Combo Prefab)
            var comboPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ComboFloatingScore.prefab");
            GameObject comboModule;
            if (comboPrefab != null)
            {
                comboModule = (GameObject)PrefabUtility.InstantiatePrefab(comboPrefab);
            }
            else
            {
                comboModule = new GameObject("ComboModule");
                comboModule.AddComponent<RectTransform>();
            }
            comboModule.name = "ComboModule";
            comboModule.transform.SetParent(root.transform, false);
            var comboRect = comboModule.GetComponent<RectTransform>();
            comboRect.anchoredPosition = new Vector2(-300, 0);

            // CellScoreModule
            var cellGo = CreateScoreModule("CellScoreModule", Color.white);
            cellGo.transform.SetParent(root.transform, false);
            cellGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 0);

            // ClearScoreModule
            var clearColor = new Color(0.4f, 1f, 0.6f, 1f);
            var clearGo = CreateScoreModule("ClearScoreModule", clearColor);
            clearGo.transform.SetParent(root.transform, false);
            clearGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(280, 0);

            // 挂载 ScorePopupView
            var pv = root.AddComponent<UI.ScorePopupView>();
            var pvSO = new SerializedObject(pv);
            pvSO.FindProperty("_comboModule").objectReferenceValue = comboModule.GetComponent<UI.ComboFloatingScoreView>();
            pvSO.FindProperty("_cellScoreModule").objectReferenceValue = cellGo.GetComponent<UI.FloatingScoreTextModule>();
            pvSO.FindProperty("_clearScoreModule").objectReferenceValue = clearGo.GetComponent<UI.FloatingScoreTextModule>();
            pvSO.ApplyModifiedPropertiesWithoutUndo();

            string path = "Assets/Prefabs/UI/ScorePopup.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[CreateScorePopupPrefabs] Created: {path}");
        }

        private static GameObject CreateScoreModule(string name, Color color)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 80);

            // PrefixText "+"
            var prefixGo = new GameObject("PrefixText");
            prefixGo.transform.SetParent(go.transform, false);
            var prefixRect = prefixGo.AddComponent<RectTransform>();
            prefixRect.anchoredPosition = new Vector2(-60, 0);
            prefixRect.sizeDelta = new Vector2(40, 70);
            var prefixTxt = prefixGo.AddComponent<Text>();
            prefixTxt.text = "+";
            prefixTxt.fontSize = 48;
            prefixTxt.alignment = TextAnchor.MiddleCenter;
            prefixTxt.color = color;
            prefixTxt.font = GetFont();
            prefixTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var prefixOL = prefixGo.AddComponent<Outline>();
            prefixOL.effectColor = new Color(0, 0, 0, 0.7f);
            prefixOL.effectDistance = new Vector2(2, -2);

            // ScoreText
            var scoreTxtGo = new GameObject("ScoreText");
            scoreTxtGo.transform.SetParent(go.transform, false);
            var scoreTxtRect = scoreTxtGo.AddComponent<RectTransform>();
            scoreTxtRect.anchoredPosition = new Vector2(20, 0);
            scoreTxtRect.sizeDelta = new Vector2(150, 70);
            var scoreTxt = scoreTxtGo.AddComponent<Text>();
            scoreTxt.text = "0";
            scoreTxt.fontSize = 48;
            scoreTxt.alignment = TextAnchor.MiddleLeft;
            scoreTxt.color = color;
            scoreTxt.font = GetFont();
            scoreTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var scoreOL = scoreTxtGo.AddComponent<Outline>();
            scoreOL.effectColor = new Color(0, 0, 0, 0.7f);
            scoreOL.effectDistance = new Vector2(2, -2);

            // 挂载 FloatingScoreTextModule
            var module = go.AddComponent<UI.FloatingScoreTextModule>();
            var so = new SerializedObject(module);
            so.FindProperty("_prefixText").objectReferenceValue = prefixTxt;
            so.FindProperty("_scoreText").objectReferenceValue = scoreTxt;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static Font GetFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
