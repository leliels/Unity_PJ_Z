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
    /// 将此脚本挂载到场景中的空 GameObject 上即可。
    /// 
    /// 设计原则：
    /// - 此脚本持有需要实例化的 Prefab 引用列表
    /// - 视觉细节（字号、颜色、图标、锚点等）在各自 Prefab 内调整
    /// - 布局参数由各自 Manager 的 Prefab Inspector 管理（BoardManager 管棋盘布局，BlockSpawner 管候选区布局）
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        // ==================== Prefab 引用列表 ====================
        [Header("需要调用的 Prefab")]
        [Tooltip("BoardManager Prefab（含棋盘布局参数 + Cell/Preview Prefab 引用）")]
        [SerializeField] private GameObject _boardManagerPrefab;

        [Tooltip("BlockSpawner Prefab（含 BlockCell/CandidateSlot Prefab 引用 + 候选区参数）")]
        [SerializeField] private GameObject _blockSpawnerPrefab;

        [Tooltip("分数显示 Prefab（含 NumberImageDisplay 组件，当前分数）")]
        [SerializeField] private GameObject _scoreDisplayPrefab;

        [Tooltip("最高分显示 Prefab（含 NumberImageDisplay 组件 + 图标）")]
        [SerializeField] private GameObject _highScoreDisplayPrefab;

        [Tooltip("GameOver 面板 Prefab（含 FinalScoreText + RestartButton）")]
        [SerializeField] private GameObject _gameOverPanelPrefab;

        [Tooltip("飘字 Prefab（需含 Text + Outline 组件）")]
        [SerializeField] private GameObject _floatingScorePrefab;

        [Tooltip("重新开始按钮 Prefab（HUD 左上角）")]
        [SerializeField] private GameObject _restartButtonPrefab;

        [Tooltip("背景 Canvas Prefab（含背景图、候选区底板装饰等固定 UI）")]
        [SerializeField] private GameObject _backgroundCanvasPrefab;

        // ==================== 玩法配置 ====================
        [Header("玩法配置")]
        [Tooltip("本场景启动时传给 ScoreManager 的计分配置。可为不同模式指定不同 ScoreConfig；为空时默认加载 Resources/Configs/ScoreConfig。")]
        [SerializeField] private ScoreConfig _scoreConfig;

        // ==================== 候选区布局参数 ====================
        [Header("候选区布局")]
        [Tooltip("候选区中心的世界坐标")]
        [SerializeField] private Vector3 _candidateCenter = new Vector3(0f, -8.5f, 0f);

        [Tooltip("候选方块之间的水平间距")]
        [SerializeField] private float _candidateSpacing = 3.5f;

        [Tooltip("候选方块的缩放比例")]
        [SerializeField] private float _candidateScale = 0.35f;

        private void Awake()
        {
            ApplyLayoutConfig();
            SetupCamera();
            CreateBackgroundUI();
            CreateManagers();
            CreateUI();
        }

        /// <summary>
        /// 将 Inspector 中的候选区布局配置写入 Constants，供 BlockSpawner 使用
        /// </summary>
        private void ApplyLayoutConfig()
        {
            Utils.Constants.CandidateCenter = _candidateCenter;
            Utils.Constants.CandidateSpacing = _candidateSpacing;
            Utils.Constants.CandidateScale = _candidateScale;
        }

        // ==================== 配置相机 ====================

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = 11f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.15f, 0.12f, 0.10f, 1f);
        }

        // ==================== 创建背景 ====================

        private void CreateBackgroundUI()
        {
            if (_backgroundCanvasPrefab != null)
            {
                var bgCanvasGo = Instantiate(_backgroundCanvasPrefab);
                bgCanvasGo.name = "BackgroundCanvas";
                // 强制设为 ScreenSpaceCamera 模式，确保背景在世界空间物体之后渲染
                var bgCanvas = bgCanvasGo.GetComponent<Canvas>();
                if (bgCanvas != null)
                {
                    bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    bgCanvas.worldCamera = Camera.main;
                    bgCanvas.planeDistance = 50f;
                    bgCanvas.sortingOrder = -10;
                }
                return;
            }

            // Fallback：代码创建
            Debug.LogWarning("[SceneBootstrap] _backgroundCanvasPrefab 未配置，使用代码创建背景");
            var bgSprite = Utils.SpriteUtils.BackgroundSprite;
            if (bgSprite == null) return;

            var fallbackGo = new GameObject("BackgroundCanvas");
            var canvas = fallbackGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 50f;
            canvas.sortingOrder = -10;

            var bgScaler = fallbackGo.AddComponent<CanvasScaler>();
            bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bgScaler.referenceResolution = new Vector2(1080, 1920);
            bgScaler.matchWidthOrHeight = 1f;

            var bgImageGo = new GameObject("BackgroundImage");
            bgImageGo.transform.SetParent(fallbackGo.transform, false);

            var bgRect = bgImageGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgImageGo.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;
        }

        // ==================== 创建管理器 ====================

        private void CreateManagers()
        {
            // BoardManager：Prefab 上已包含 Cell/Preview Prefab 引用
            if (FindFirstObjectByType<BoardManager>() == null)
            {
                if (_boardManagerPrefab != null)
                {
                    var boardGo = Instantiate(_boardManagerPrefab);
                    boardGo.name = "[BoardManager]";
                    boardGo.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[SceneBootstrap] _boardManagerPrefab 未配置，使用默认参数创建 BoardManager");
                    var boardGo = new GameObject("[BoardManager]");
                    boardGo.AddComponent<BoardManager>();
                }
            }

            // BlockSpawner：Prefab 上已包含 BlockCell/CandidateSlot/底板等引用
            if (FindFirstObjectByType<BlockSpawner>() == null)
            {
                if (_blockSpawnerPrefab != null)
                {
                    var spawnerGo = Instantiate(_blockSpawnerPrefab);
                    spawnerGo.name = "[BlockSpawner]";
                    spawnerGo.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[SceneBootstrap] _blockSpawnerPrefab 未配置，使用默认参数创建 BlockSpawner");
                    var spawnerGo = new GameObject("[BlockSpawner]");
                    spawnerGo.AddComponent<BlockSpawner>();
                }
            }

            // ScoreManager
            var scoreManager = FindFirstObjectByType<ScoreManager>();
            if (scoreManager == null)
            {
                var scoreGo = new GameObject("[ScoreManager]");
                scoreManager = scoreGo.AddComponent<ScoreManager>();
            }
            scoreManager.SetConfig(_scoreConfig);

            // GameManager（最后创建）
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

            // --- 分数显示（从 Prefab 实例化） ---
            GameObject scoreDisplayGo = InstantiatePrefab(_scoreDisplayPrefab, canvasGo.transform, "ScoreDisplay");
            EnsureDigitSprites(scoreDisplayGo);

            // --- 最高分显示（从 Prefab 实例化） ---
            GameObject highScoreDisplayGo = InstantiatePrefab(_highScoreDisplayPrefab, canvasGo.transform, "HighScoreDisplay");
            EnsureDigitSprites(highScoreDisplayGo);

            // --- 游戏结束面板（从 Prefab 实例化） ---
            GameObject gameOverPanel = InstantiatePrefab(_gameOverPanelPrefab, canvasGo.transform, "GameOverPanel");
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // --- 挂载 GameUI 脚本 ---
            var gameUI = canvasGo.AddComponent<GameUI>();

            SetPrivateField(gameUI, "_scoreDisplay", scoreDisplayGo?.GetComponent<NumberImageDisplay>());
            SetPrivateField(gameUI, "_highScoreDisplay", highScoreDisplayGo?.GetComponent<NumberImageDisplay>());
            SetPrivateField(gameUI, "_gameOverPanel", gameOverPanel);
            SetPrivateField(gameUI, "_finalScoreText", gameOverPanel?.transform.Find("FinalScoreText")?.GetComponent<Text>());
            SetPrivateField(gameUI, "_restartButton", gameOverPanel?.transform.Find("RestartButton")?.GetComponent<Button>());

            // --- HUD 重新开始按钮 ---
            if (_restartButtonPrefab != null)
            {
                var hudRestartGo = Instantiate(_restartButtonPrefab, canvasGo.transform, false);
                hudRestartGo.name = "HudRestartButton";
                var hudBtn = hudRestartGo.GetComponent<Button>();
                if (hudBtn == null) hudBtn = hudRestartGo.AddComponent<Button>();
                hudBtn.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.RestartGame();
                });
            }

            // --- 得分飘字管理器 ---
            var floatingMgr = canvasGo.AddComponent<FloatingScoreManager>();
            floatingMgr.Init(canvas);
            floatingMgr.SetFloatingScorePrefab(_floatingScorePrefab);

            // 监听消除计分事件，驱动飘字
            if (Score.ScoreManager.Instance != null)
            {
                var scoreManager = Score.ScoreManager.Instance;
                scoreManager.OnLineClearScoreDetail += (lineCount, baseScore, comboBonus, comboCount) =>
                {
                    floatingMgr.EnqueuePlacementScore(scoreManager.LastPlacementScore);
                    floatingMgr.EnqueueClearScore(baseScore, lineCount);
                    if (comboBonus > 0)
                        floatingMgr.EnqueueComboBonus(comboCount, comboBonus);
                    floatingMgr.PlayAll();
                };


                floatingMgr.OnAllFinished += () =>
                {
                    var ui = FindFirstObjectByType<GameUI>();
                    if (ui != null)
                    {
                        var scoreField = typeof(GameUI).GetField("_scoreDisplay",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var display = scoreField?.GetValue(ui) as NumberImageDisplay;
                        display?.PlayBounceEffect();
                    }
                };
            }
        }

        // ==================== 辅助方法 ====================

        /// <summary>实例化 Prefab 到指定父节点下</summary>
        private GameObject InstantiatePrefab(GameObject prefab, Transform parent, string fallbackName)
        {
            if (prefab != null)
            {
                var go = Instantiate(prefab, parent, false);
                go.name = fallbackName;
                go.SetActive(true);
                return go;
            }
            Debug.LogWarning($"[SceneBootstrap] Prefab 未配置: {fallbackName}，请在 Inspector 中设置");
            return null;
        }

        /// <summary>自动加载数字精灵到 NumberImageDisplay（如果 Prefab 没有配置）</summary>
        private void EnsureDigitSprites(GameObject scoreGo)
        {
            if (scoreGo == null) return;
            var display = scoreGo.GetComponent<NumberImageDisplay>();
            if (display == null || display.HasValidSprites) return;

            var sprites = new Sprite[10];
            bool allLoaded = true;
            for (int i = 0; i <= 9; i++)
            {
                sprites[i] = Resources.Load<Sprite>($"Digits/SH2_{i}");
                if (sprites[i] == null) allLoaded = false;
            }

            if (allLoaded)
                SetPrivateField(display, "_numberSprites", sprites);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            if (obj == null) return;
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }

        // ==================== 运行时参数热更新 ====================

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyLayoutConfig();

            if (!Application.isPlaying) return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                // 刷新候选区布局
                if (BlockSpawner.Instance != null)
                    BlockSpawner.Instance.RelayoutCandidates();
            };
        }
#endif

    }
}
