using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Audio;
using BlockPuzzle.Board;
using BlockPuzzle.Block;
using BlockPuzzle.Config;
using BlockPuzzle.Feedback;
using BlockPuzzle.Mode;
using BlockPuzzle.Save;
using BlockPuzzle.Score;
using BlockPuzzle.UI;
using BlockPuzzle.UI.Layout;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// 场景启动器(M-R2/R3 重写,UGUI 化版本)。
    ///
    /// 启动流程:
    ///   1. 加载 GameConfig.asset(M-R1 已落地)
    ///   2. 配置主相机
    ///   3. 用代码搭出 4 个 Canvas(背景/玩法/HUD/弹窗)
    ///   4. 在 PlayCanvas 下生成 BoardRoot + CandidateRoot
    ///   5. 实例化 Manager Prefab + 注入 RectTransform / Config
    ///   6. 启动 GameManager 进入对局
    ///
    /// Inspector 字段精简到只剩"必须由场景给出的 Prefab 引用"。其它布局/数值参数走 GameConfig。
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        // ==================== Prefab 引用 ====================
        [Header("管理器 Prefab(可选)")]
        [Tooltip("BoardManager Prefab。为空时,Bootstrap 会在 PlayCanvas 下挂一个空 BoardManager。")]
        [SerializeField] private GameObject _boardManagerPrefab;

        [Tooltip("BlockSpawner Prefab。为空时同 BoardManager。")]
        [SerializeField] private GameObject _blockSpawnerPrefab;

        [Header("UI Prefab(可选)")]
        [Tooltip("背景图 Prefab(包含 Image 组件,用于 BackgroundCanvas)。为空时回退到 Resources/Art/Backgrounds/bg_game。")]
        [SerializeField] private GameObject _backgroundPrefab;

        [Tooltip("分数显示 Prefab(含 NumberImageDisplay)。挂在 HudCanvas 上。")]
        [SerializeField] private GameObject _scoreDisplayPrefab;

        [Tooltip("最高分显示 Prefab。挂在 HudCanvas 上。")]
        [SerializeField] private GameObject _highScoreDisplayPrefab;

        [Tooltip("GameOver 面板 Prefab。挂在 OverlayCanvas 上。")]
        [SerializeField] private GameObject _gameOverPanelPrefab;

        [Tooltip("飘字 Prefab(老版,含 Text + Outline,可选)。")]
        [SerializeField] private GameObject _floatingScorePrefab;

        [Tooltip("结构化飘字 Prefab(含 ScorePopupView)。")]
        [SerializeField] private GameObject _scorePopupPrefab;

        [Header("棋盘渲染 Prefab")]
        [Tooltip("背景格子 Prefab(BoardBackground 层)。控制棋盘底格外观(精灵/颜色/圆角等)。为空时不生成背景格。")]
        [SerializeField] private GameObject _uiCellPrefab;

        [Tooltip("逻辑格子 Prefab(Cells 层)。放上方块后显示的格子外观。为空时代码 fallback(纯 Image,无精灵)。")]
        [SerializeField] private GameObject _uiLogicCellPrefab;

        [Tooltip("UI 放置预览 Prefab(可选,Image 类型)。")]
        [SerializeField] private GameObject _uiPreviewPrefab;

        [Header("候选区 Prefab")]
        [Tooltip("候选槽位 Prefab(RectTransform + Image)。")]
        [SerializeField] private GameObject _candidateSlotPrefab;

        [Tooltip("方块单格 Prefab(Image)。")]
        [SerializeField] private GameObject _blockCellPrefab;

        // ==================== 全局缓存 ====================
        public static GameConfig ActiveConfig { get; private set; }

        // 构建出来的 Canvas / Root,供其它系统读取
        public static Canvas BackgroundCanvas { get; private set; }
        public static Canvas PlayCanvas { get; private set; }
        public static Canvas HudCanvas { get; private set; }
        public static Canvas OverlayCanvas { get; private set; }
        public static RectTransform BoardRoot { get; private set; }
        public static RectTransform CandidateRoot { get; private set; }
        public static RectTransform HudSafeRoot { get; private set; }
        public static RectTransform OverlaySafeRoot { get; private set; }

        // ==================== 启动 ====================

        private void Awake()
        {
            LoadGameConfig();
            SetupCamera();
            EnsureEventSystem();
            BuildCanvases();
            BuildBoardLayout();
            CreateManagers();
            CreateUI();
            CreateSelfServiceManagers();
        }

        private void LoadGameConfig()
        {
            ActiveConfig = GameConfig.LoadFromResources();
            if (ActiveConfig == null)
            {
                Debug.LogWarning("[SceneBootstrap] 未找到 GameConfig.asset。建议菜单 BlockPuzzle/游戏配置中心 创建。" +
                                 "本次启动使用 Inspector / 默认值回退。");
                return;
            }

            var missing = ActiveConfig.ValidateRuntime();
            if (missing.Length > 0)
                Debug.LogWarning("[SceneBootstrap] GameConfig 缺字段:" + string.Join(", ", missing));
            else
                Debug.Log("[SceneBootstrap] GameConfig 加载成功:" + ActiveConfig.name);

            if (ActiveConfig.Score != null)
                Debug.Log("[SceneBootstrap] 使用 GameConfig 中的 ScoreConfig");
            else
                Debug.LogWarning("[SceneBootstrap] GameConfig 中未配置 ScoreConfig,ScoreManager 将使用默认值。");
        }

        // ==================== 相机 ====================

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            // orthographicSize 不再硬编码用于布局,仅作占位(背景纯色)
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        // ==================== 4 Canvas ====================

        private void BuildCanvases()
        {
            var refRes = ActiveConfig?.Layout != null ? ActiveConfig.Layout.ReferenceResolution : new Vector2(1080f, 1920f);
            float match = ActiveConfig?.Layout != null ? ActiveConfig.Layout.MatchWidthOrHeight : 0.5f;
            var theme = ActiveConfig?.Theme;

            // BackgroundCanvas: 如果 Prefab 自带 Canvas 组件,直接实例化整个 Prefab 作为 BackgroundCanvas
            if (_backgroundPrefab != null && _backgroundPrefab.GetComponent<Canvas>() != null)
            {
                var bgGo = Instantiate(_backgroundPrefab);
                bgGo.name = "BackgroundCanvas";
                var bgCanvas = bgGo.GetComponent<Canvas>();
                // 确保 Camera 引用正确(Prefab 里可能没配)
                if (bgCanvas.renderMode == RenderMode.ScreenSpaceCamera && bgCanvas.worldCamera == null)
                    bgCanvas.worldCamera = Camera.main;
                // 确保 sortingOrder 最低,渲染在所有 Canvas 最后面
                bgCanvas.sortingOrder = Mathf.Min(bgCanvas.sortingOrder, -1);
                // 移除 GraphicRaycaster 避免拦截点击事件
                var raycaster = bgGo.GetComponent<GraphicRaycaster>();
                if (raycaster != null) Destroy(raycaster);
                BackgroundCanvas = bgCanvas;
            }
            else
            {
                BackgroundCanvas = BuildCanvas("BackgroundCanvas", RenderMode.ScreenSpaceCamera, sortOrder: 0, refRes, match);
                CreateBackgroundImage(BackgroundCanvas);
            }

            PlayCanvas = BuildCanvas("PlayCanvas", RenderMode.ScreenSpaceOverlay, sortOrder: 10, refRes, match);
            HudCanvas = BuildCanvas("HudCanvas", RenderMode.ScreenSpaceOverlay, sortOrder: 20, refRes, match);
            OverlayCanvas = BuildCanvas("OverlayCanvas", RenderMode.ScreenSpaceOverlay, sortOrder: 30, refRes, match);

            // HUD / Overlay 各放一个 SafeAreaRoot,后续所有 HUD 内容都进 SafeAreaRoot
            HudSafeRoot = CreateSafeAreaRoot(HudCanvas, theme, isHud: true);
            OverlaySafeRoot = CreateSafeAreaRoot(OverlayCanvas, theme, isHud: false);
        }

        private Canvas BuildCanvas(string name, RenderMode mode, int sortOrder, Vector2 refRes, float match)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = mode;
            canvas.sortingOrder = sortOrder;

            if (mode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 10f + sortOrder; // 背景 / 玩法 平面距离 错开,避免穿插
            }

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = refRes;
            scaler.matchWidthOrHeight = match;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void CreateBackgroundImage(Canvas bgCanvas)
        {
            GameObject bg;
            if (_backgroundPrefab != null)
            {
                bg = Instantiate(_backgroundPrefab, bgCanvas.transform, false);
                bg.name = "Background";
            }
            else
            {
                bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(bgCanvas.transform, false);
                var img = bg.GetComponent<Image>();
                img.sprite = Utils.SpriteUtils.BackgroundSprite;
                img.raycastTarget = false;
                img.preserveAspect = false;
            }
            var rt = bg.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private RectTransform CreateSafeAreaRoot(Canvas parentCanvas, UIThemeConfig theme, bool isHud)
        {
            var go = new GameObject("SafeAreaRoot", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parentCanvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 是否挂 SafeAreaFitter,看 UIThemeConfig 策略
            UIThemeConfig.SafeAreaPolicy policy = theme != null ? theme.SafeAreaMode : UIThemeConfig.SafeAreaPolicy.HudAndOverlay;
            bool apply = policy switch
            {
                UIThemeConfig.SafeAreaPolicy.None => false,
                UIThemeConfig.SafeAreaPolicy.HudOnly => isHud,
                UIThemeConfig.SafeAreaPolicy.OverlayOnly => !isHud,
                UIThemeConfig.SafeAreaPolicy.HudAndOverlay => true,
                _ => true,
            };
            if (apply) go.AddComponent<SafeAreaFitter>();
            return rt;
        }

        // ==================== 棋盘布局(Board / Candidate Root) ====================

        private void BuildBoardLayout()
        {
            var layoutConfig = ActiveConfig?.Layout;
            float topMargin = layoutConfig != null ? layoutConfig.BoardMarginTopRatio : 0.18f;
            float bottomMargin = layoutConfig != null ? layoutConfig.BoardMarginBottomRatio : 0.30f;
            float candidateBottom = layoutConfig != null ? layoutConfig.CandidateBottomMarginRatio : 0.05f;

            // ==================== BoardRoot ====================
            // 策略:用 anchor 在屏幕中"刨"出一块矩形区域(顶部留白~底部留白 = 棋盘可占空间),
            // BoardRoot 自己用 AspectRatioFitter 在这块区域里居中并锁 1:1。
            // 这样在任意宽高比下,只要可占空间够,棋盘永远是正方形居中。
            //
            // 关键:不能像旧实现那样 anchorMin.y == anchorMax.y(让 anchor 是一条线),
            // 必须 anchor 是一块面积,才能让父级 RectTransform 计算出宽度让 AspectRatioFitter 用。

            // 第一步:外层"棋盘可用区"(由 LayoutConfig 留白决定),anchor 撑出实际矩形
            var slotGo = new GameObject("BoardSlot", typeof(RectTransform));
            var boardSlot = slotGo.GetComponent<RectTransform>();
            boardSlot.SetParent(PlayCanvas.transform, false);
            boardSlot.anchorMin = new Vector2(0f, bottomMargin);
            boardSlot.anchorMax = new Vector2(1f, 1f - topMargin);
            boardSlot.offsetMin = Vector2.zero;
            boardSlot.offsetMax = Vector2.zero;
            boardSlot.localScale = Vector3.one;

            // 第二步:BoardRoot 是 BoardSlot 的子节点,挂 AspectRatioFitter,
            // FitInParent 会自动计算最大可放下的 1:1 矩形并居中。
            var boardGo = new GameObject("BoardRoot", typeof(RectTransform));
            BoardRoot = boardGo.GetComponent<RectTransform>();
            BoardRoot.SetParent(boardSlot, false);
            BoardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            BoardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            BoardRoot.pivot = new Vector2(0.5f, 0.5f);
            BoardRoot.localScale = Vector3.one;

            // 应用棋盘位置偏移(LayoutConfig.BoardOffsetXRatio / BoardOffsetYRatio)
            if (layoutConfig != null)
            {
                var canvasRt = (RectTransform)PlayCanvas.transform;
                float screenW = canvasRt.rect.width;
                float screenH = canvasRt.rect.height;
                // Canvas 可能还没完成布局(rect 为 0),此时退回参考分辨率
                if (screenW <= 0f) screenW = layoutConfig.ReferenceResolution.x;
                if (screenH <= 0f) screenH = layoutConfig.ReferenceResolution.y;
                float offX = layoutConfig.BoardOffsetXRatio * screenW;
                float offY = layoutConfig.BoardOffsetYRatio * screenH;
                BoardRoot.anchoredPosition = new Vector2(offX, offY);
            }
            else
            {
                BoardRoot.anchoredPosition = Vector2.zero;
            }

            var fitter = boardGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            // ==================== CandidateRoot ====================
            var candGo = new GameObject("CandidateRoot", typeof(RectTransform));
            CandidateRoot = candGo.GetComponent<RectTransform>();
            CandidateRoot.SetParent(PlayCanvas.transform, false);
            CandidateRoot.anchorMin = new Vector2(0.05f, candidateBottom);
            CandidateRoot.anchorMax = new Vector2(0.95f, bottomMargin);
            CandidateRoot.offsetMin = Vector2.zero;
            CandidateRoot.offsetMax = Vector2.zero;
            CandidateRoot.localScale = Vector3.one;
        }

        // ==================== Manager 创建 ====================

        private void CreateManagers()
        {
            EnsureGlobalServices();

            CreateBoardManager();
            CreateBlockSpawner();
            CreateScoreManager();
            CreateGameManager();
        }

        private void EnsureGlobalServices()
        {
            _ = SaveManager.Instance;
            _ = ModeManager.Instance;
            var audio = AudioManager.Instance;
            _ = FeedbackManager.Instance;
            audio?.PlayGameBgm();
        }

        private void CreateBoardManager()
        {
            var existing = FindFirstObjectByType<BoardManager>();
            if (existing == null)
            {
                if (_boardManagerPrefab != null)
                {
                    var go = Instantiate(_boardManagerPrefab);
                    go.name = "[BoardManager]";
                    existing = go.GetComponent<BoardManager>();
                }
                else
                {
                    var go = new GameObject("[BoardManager]");
                    existing = go.AddComponent<BoardManager>();
                }
            }
            // 注入 BoardRoot
            InjectField(existing, "_boardRoot", BoardRoot);
            InjectField(existing, "_uiCellPrefab", _uiCellPrefab);
            InjectField(existing, "_uiLogicCellPrefab", _uiLogicCellPrefab);
            InjectField(existing, "_uiPreviewPrefab", _uiPreviewPrefab);
            existing.Configure(ActiveConfig?.Layout, ActiveConfig?.Theme);
        }

        private void CreateBlockSpawner()
        {
            var existing = FindFirstObjectByType<BlockSpawner>();
            if (existing == null)
            {
                if (_blockSpawnerPrefab != null)
                {
                    var go = Instantiate(_blockSpawnerPrefab);
                    go.name = "[BlockSpawner]";
                    existing = go.GetComponent<BlockSpawner>();
                }
                else
                {
                    var go = new GameObject("[BlockSpawner]");
                    existing = go.AddComponent<BlockSpawner>();
                }
            }
            InjectField(existing, "_candidateRoot", CandidateRoot);
            InjectField(existing, "_slotPrefab", _candidateSlotPrefab);
            InjectField(existing, "_blockCellPrefab", _blockCellPrefab);

            // 形状库
            if (ActiveConfig?.Shapes != null)
                existing.SetShapeDatabase(ActiveConfig.Shapes);

            existing.Configure(ActiveConfig?.Layout, ActiveConfig?.Theme);
        }

        private void CreateScoreManager()
        {
            var sm = FindFirstObjectByType<ScoreManager>();
            if (sm == null)
            {
                var go = new GameObject("[ScoreManager]");
                sm = go.AddComponent<ScoreManager>();
            }
            sm.SetConfig(ActiveConfig?.Score);
        }

        private void CreateGameManager()
        {
            if (FindFirstObjectByType<GameManager>() == null)
            {
                var go = new GameObject("[GameManager]");
                go.AddComponent<GameManager>();
            }
        }

        // ==================== UI ====================

        private void CreateUI()
        {
            // HUD: 分数 / 最高分 进 HudSafeRoot
            var scoreGo = InstantiateInto(_scoreDisplayPrefab, HudSafeRoot, "ScoreDisplay");
            EnsureDigitSprites(scoreGo);
            var highScoreGo = InstantiateInto(_highScoreDisplayPrefab, HudSafeRoot, "HighScoreDisplay");
            EnsureDigitSprites(highScoreGo);

            // OverlayCanvas: GameOverPanel
            var goPanel = InstantiateInto(_gameOverPanelPrefab, OverlaySafeRoot, "GameOverPanel");
            if (goPanel != null) goPanel.SetActive(false);

            // GameUI / GameFlowUI 挂在 HudCanvas 根
            var gameUI = HudCanvas.gameObject.AddComponent<GameUI>();
            HudCanvas.gameObject.AddComponent<GameFlowUI>();

            if (FindFirstObjectByType<GameplayAudioBinder>() == null)
            {
                var go = new GameObject("[GameplayAudioBinder]");
                go.AddComponent<GameplayAudioBinder>();
            }

            InjectField(gameUI, "_scoreDisplay", scoreGo?.GetComponent<NumberImageDisplay>());
            InjectField(gameUI, "_highScoreDisplay", highScoreGo?.GetComponent<NumberImageDisplay>());
            InjectField(gameUI, "_gameOverPanel", goPanel);
            if (goPanel != null)
            {
                InjectField(gameUI, "_finalScoreText", goPanel.transform.Find("FinalScoreText")?.GetComponent<Text>());
                InjectField(gameUI, "_restartButton", goPanel.transform.Find("RestartButton")?.GetComponent<Button>());
            }

            // 飘字管理器:挂在 HudCanvas
            var floating = HudCanvas.gameObject.AddComponent<FloatingScoreManager>();
            floating.Init(HudCanvas);
            floating.SetFloatingScorePrefab(_floatingScorePrefab);
            floating.SetScorePopupPrefab(_scorePopupPrefab);

            if (Score.ScoreManager.Instance != null)
            {
                var sm = Score.ScoreManager.Instance;
                sm.OnLineClearScoreDetail += (lineCount, cellScore, clearComboScore, comboCount) =>
                {
                    floating.EnqueueClearScore(comboCount, cellScore, clearComboScore);
                    floating.PlayAll();
                };
                sm.OnPlaceScore += (placeScore) =>
                {
                    floating.EnqueuePlaceScore(placeScore);
                    floating.PlayAll();
                };
                floating.OnAllFinished += () =>
                {
                    gameUI.PlayScoreBounce();
                };
            }
        }

        private GameObject InstantiateInto(GameObject prefab, Transform parent, string name)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[SceneBootstrap] Prefab 未配置: {name}");
                return null;
            }
            var go = Instantiate(prefab, parent, false);
            go.name = name;
            go.SetActive(true);
            return go;
        }

        private void EnsureDigitSprites(GameObject scoreGo)
        {
            if (scoreGo == null) return;
            var display = scoreGo.GetComponent<NumberImageDisplay>();
            if (display == null || display.HasValidSprites) return;
            var sprites = new Sprite[10];
            bool ok = true;
            for (int i = 0; i <= 9; i++)
            {
                sprites[i] = Resources.Load<Sprite>($"Digits/SH2_{i}");
                if (sprites[i] == null) ok = false;
            }
            if (ok) InjectField(display, "_numberSprites", sprites);
        }

        private static void InjectField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var f = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && value != null) f.SetValue(target, value);
        }

        // ==================== M-R5 自助体系 Manager ====================

        public static RectTransform FxLayer { get; private set; }
        public static RectTransform FloatingTextLayer { get; private set; }

        private void CreateSelfServiceManagers()
        {
            // 反射式按 SO 名查 FxLibrary / FloatingTextLibrary / AudioBindings
            // (M-R5 阶段 GameConfig.cs 里这三个字段是后加的,我们优先用 Resources 直接加载,避免重新做配置中心)
            var fxLib = Resources.Load<FxLibrary>("Configs/02_Feel/FxLibrary");
            var ftLib = Resources.Load<FloatingTextLibrary>("Configs/02_Feel/FloatingTextLibrary");
            var bindings = Resources.Load<AudioBindings>("Configs/02_Feel/AudioBindings");

            // 1. 在 PlayCanvas 下加 FxLayer(覆盖在棋盘和候选区之上)
            FxLayer = CreateLayer(PlayCanvas.transform, "FxLayer", siblingLast: true);

            // 2. 在 HudCanvas 下加 FloatingTextLayer(放分数飘字)
            FloatingTextLayer = CreateLayer(HudCanvas.transform, "FloatingTextLayer", siblingLast: true);

            var tuning = ActiveConfig?.Gameplay;

            // 3. FxManager
            if (fxLib != null)
            {
                var fxGo = new GameObject("[FxManager]");
                var fx = fxGo.AddComponent<FxManager>();
                fx.Init(fxLib, tuning, FxLayer);
            }

            // 4. FloatingTextManager
            if (ftLib != null)
            {
                var ftGo = new GameObject("[FloatingTextManager]");
                var ft = ftGo.AddComponent<FloatingTextManager>();
                ft.Init(ftLib, FloatingTextLayer);
            }

            // 5. GameplayEventAudioBinder
            if (bindings != null)
            {
                var gbGo = new GameObject("[GameplayEventAudioBinder]");
                var gb = gbGo.AddComponent<GameplayEventAudioBinder>();
                gb.Init(bindings, tuning);
            }
        }

        private static RectTransform CreateLayer(Transform parent, string name, bool siblingLast)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            if (siblingLast) rt.SetAsLastSibling();
            return rt;
        }
    }
}
