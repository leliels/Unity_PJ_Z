using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core;
using BlockPuzzle.Score;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 游戏 HUD：分数显示 + 游戏结束面板
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private NumberImageDisplay _scoreDisplay;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _finalScoreText;
        [SerializeField] private Button _restartButton;

        private void Start()
        {
            // 注册事件
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            // 重新开始按钮
            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestartClicked);

            // 初始状态
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);

            UpdateScoreDisplay(0);
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
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
                    _finalScoreText.text = $"Final Score\n{ScoreManager.Instance.CurrentScore}";
            }
        }

        private void HideGameOver()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);
        }

        private void OnRestartClicked()
        {
            GameManager.Instance.RestartGame();
        }
    }
}
