using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 分数文本模块：显示 "+分数" 样式的飘字子组件。
    /// Prefab 内部结构：
    ///   - PrefixText (Text/Image)：显示 "+" 前缀，可替换为艺术字图片
    ///   - ScoreText (Text)：显示分数数字，字体/字号/颜色/描边在 Inspector 中配置
    /// 代码只调用 SetScore() 设置数字，所有视觉样式由 Prefab 决定。
    /// </summary>
    public class FloatingScoreTextModule : MonoBehaviour
    {
        [Header("前缀（可选，用于显示 '+' 符号）")]
        [Tooltip("前缀 Text 组件，显示 '+' 等符号。也可用 Image 替代。")]
        [SerializeField] private Text _prefixText;

        [Header("分数文本")]
        [Tooltip("分数 Text 组件，字体/字号/颜色/描边在此组件上配置")]
        [SerializeField] private Text _scoreText;

        /// <summary>
        /// 设置分数数值。前缀由 Prefab 配置（默认 "+"），此方法只设置数字部分。
        /// </summary>
        public void SetScore(long score)
        {
            if (_scoreText != null)
                _scoreText.text = score.ToString();
        }

        /// <summary>
        /// 设置完整文本（含前缀），用于 fallback 场景。
        /// </summary>
        public void SetFullText(string text)
        {
            if (_scoreText != null)
                _scoreText.text = text;

            // 隐藏前缀，因为文本已包含
            if (_prefixText != null)
                _prefixText.gameObject.SetActive(false);
        }

        /// <summary>
        /// 获取分数文本的颜色（用于动画淡出）
        /// </summary>
        public Color GetColor()
        {
            return _scoreText != null ? _scoreText.color : Color.white;
        }

        /// <summary>
        /// 设置分数文本的透明度（用于动画淡出）
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_scoreText != null)
            {
                var c = _scoreText.color;
                _scoreText.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (_prefixText != null)
            {
                var c = _prefixText.color;
                _prefixText.color = new Color(c.r, c.g, c.b, alpha);
            }
        }
    }
}
