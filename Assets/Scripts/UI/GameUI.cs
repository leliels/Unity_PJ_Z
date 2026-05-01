using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core;
using BlockPuzzle.Score;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 游戏 HUD：分数显示 + 最高分显示 + 游戏结束面板
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private NumberImageDisplay _scoreDisplay;

        [Header("最高分")]
        [SerializeField] private NumberImageDisplay _highScoreDisplay;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _finalScoreText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _returnTitleButton;

        private ScoreManager _scoreManager;
        private GameManager _gameManager;

        private void Start()
        {
            // 注册事件
            _scoreManager = ScoreManager.Current ?? ScoreManager.Instance;
            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged += UpdateScoreDisplay;
                _scoreManager.OnHighScoreChanged += UpdateHighScoreDisplay;
                // 初始显示最高分
                UpdateHighScoreDisplay(_scoreManager.HighScore);
            }

            _gameManager = GameManager.Current ?? GameManager.Instance;
            if (_gameManager != null)
                _gameManager.OnGameStateChanged += HandleGameStateChanged;

            EnsureGameOverButtons();

            // 重新开始按钮
            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestartClicked);
            if (_returnTitleButton != null)
                _returnTitleButton.onClick.AddListener(OnReturnTitleClicked);

            // 初始状态
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);

            UpdateScoreDisplay(0);
        }

        private void OnDestroy()
        {
            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged -= UpdateScoreDisplay;
                _scoreManager.OnHighScoreChanged -= UpdateHighScoreDisplay;
                _scoreManager = null;
            }

            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= HandleGameStateChanged;
                _gameManager = null;
            }

            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            if (_returnTitleButton != null)
                _returnTitleButton.onClick.RemoveListener(OnReturnTitleClicked);
        }

        private void UpdateHighScoreDisplay(int highScore)
        {
            if (_highScoreDisplay != null)
                _highScoreDisplay.SetNumber(highScore);
        }

        private void UpdateScoreDisplay(int score)
        {
            if (_scoreDisplay != null)
                _scoreDisplay.SetNumber(score);
        }

        /// <summary>
        /// 外部调用：用当前格式刷新分数显示（热更新用）
        /// </summary>
        public void RefreshDisplay(int currentScore)
        {
            UpdateScoreDisplay(currentScore);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                ShowGameOver();
            }
            else if (state == GameState.Playing)
            {
                HideGameOver();
            }
        }

        private void ShowGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);

                if (_finalScoreText != null)
                {
                    int score = _scoreManager != null ? _scoreManager.CurrentScore : 0;
                    int highScore = _scoreManager != null ? _scoreManager.HighScore : 0;
                    _finalScoreText.text = $"游戏结束\n本局得分：{score}\n最高分：{highScore}";
                }
            }
        }

        private void HideGameOver()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);
        }

        private void EnsureGameOverButtons()
        {
            if (_gameOverPanel == null) return;

            if (_returnTitleButton == null)
            {
                var existing = _gameOverPanel.transform.Find("ReturnTitleButton");
                _returnTitleButton = existing != null ? existing.GetComponent<Button>() : null;
            }

            if (_returnTitleButton == null)
            {
                _returnTitleButton = RuntimeUiFactory.CreateButton(_gameOverPanel.transform, "ReturnTitleButton", "返回 Title", new Vector2(300f, 72f), new Color(0.35f, 0.35f, 0.45f, 1f));
                _returnTitleButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -190f);
            }
        }

        private void OnRestartClicked()
        {
            (_gameManager != null ? _gameManager : GameManager.Current)?.RestartGame();
        }

        private void OnReturnTitleClicked()
        {
            (_gameManager != null ? _gameManager : GameManager.Current)?.ReturnToTitle();
        }
    }
}
