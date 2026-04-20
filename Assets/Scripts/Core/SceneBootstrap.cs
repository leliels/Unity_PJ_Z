using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Board;
using BlockPuzzle.Block;
using BlockPuzzle.Score;
using BlockPuzzle.UI;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// 场景启动器：自动创建并配置所有运行时所需的管理器和UI。
    /// 将此脚本挂载到场景中的任意空 GameObject 上即可。
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            SetupCamera();
            CreateManagers();
            CreateUI();
        }

        // ==================== 配置相机 ====================

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = 11f; // 竖屏：上下各11个世界单位
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f); // 深色背景
        }

        // ==================== 创建管理器 ====================

        private void CreateManagers()
        {
            // BoardManager
            if (FindFirstObjectByType<BoardManager>() == null)
            {
                var boardGo = new GameObject("[BoardManager]");
                boardGo.AddComponent<BoardManager>();
            }

            // BlockSpawner
            if (FindFirstObjectByType<BlockSpawner>() == null)
            {
                var spawnerGo = new GameObject("[BlockSpawner]");
                spawnerGo.AddComponent<BlockSpawner>();
            }

            // ScoreManager
            if (FindFirstObjectByType<ScoreManager>() == null)
            {
                var scoreGo = new GameObject("[ScoreManager]");
                scoreGo.AddComponent<ScoreManager>();
            }

            // GameManager（最后创建，因为 Start 中会初始化各系统）
            if (FindFirstObjectByType<GameManager>() == null)
            {
                var gmGo = new GameObject("[GameManager]");
                gmGo.AddComponent<GameManager>();
            }
        }

        // ==================== 创建 UI ====================

        private void CreateUI()
        {
            // 创建 Canvas
            var canvasGo = new GameObject("GameCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // --- 分数文本 ---
            var scoreGo = CreateUIText(canvasGo.transform, "ScoreText", "Score: 0",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -40), new Vector2(400, 80), 48, TextAnchor.MiddleCenter, Color.white);

            // --- 游戏结束面板 ---
            var gameOverPanel = CreateGameOverPanel(canvasGo.transform);

            // --- 挂载 GameUI 脚本 ---
            var gameUI = canvasGo.AddComponent<GameUI>();

            // 通过反射设置私有字段（因为是 SerializeField）
            SetPrivateField(gameUI, "_scoreText", scoreGo.GetComponent<Text>());
            SetPrivateField(gameUI, "_gameOverPanel", gameOverPanel);
            SetPrivateField(gameUI, "_finalScoreText", gameOverPanel.transform.Find("FinalScoreText").GetComponent<Text>());
            SetPrivateField(gameUI, "_restartButton", gameOverPanel.transform.Find("RestartButton").GetComponent<Button>());
        }

        private GameObject CreateGameOverPanel(Transform parent)
        {
            // 半透明背景面板
            var panelGo = new GameObject("GameOverPanel");
            panelGo.transform.SetParent(parent, false);

            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // 最终分数文本
            CreateUIText(panelGo.transform, "FinalScoreText", "Final Score\n0",
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                Vector2.zero, new Vector2(600, 200), 56, TextAnchor.MiddleCenter, Color.white);

            // 重新开始按钮
            var btnGo = new GameObject("RestartButton");
            btnGo.transform.SetParent(panelGo.transform, false);

            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.35f);
            btnRect.anchorMax = new Vector2(0.5f, 0.35f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = new Vector2(350, 100);

            var btnImage = btnGo.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.7f, 0.4f, 1f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImage;

            // 按钮文字
            CreateUIText(btnGo.transform, "BtnText", "Restart",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 40, TextAnchor.MiddleCenter, Color.white, true);

            panelGo.SetActive(false);
            return panelGo;
        }

        // ==================== UI 工具方法 ====================

        private GameObject CreateUIText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta, int fontSize,
            TextAnchor alignment, Color color, bool stretch = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            if (!stretch) rect.sizeDelta = sizeDelta;
            else
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            // Unity 6 使用 LegacyRuntime.ttf，旧版使用 Arial.ttf
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return go;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }
    }
}
