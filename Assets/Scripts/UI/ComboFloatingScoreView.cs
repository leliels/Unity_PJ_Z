using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// Combo 独立模块控制器。
    /// Prefab 内部结构：
    ///   - ComboLabel (Text/Image)：显示 "Combo ×" 标签，可替换为艺术字
    ///   - MultiplierDisplay (NumberImageDisplay 或 Text)：显示倍率纯数字（2、3、4...）
    /// 代码只调用 SetCombo(int) 设置倍率数值，视觉样式完全由 Prefab 配置。
    /// </summary>
    public class ComboFloatingScoreView : MonoBehaviour
    {
        [Header("Combo 标签（显示 'Combo ×' 文字或图片）")]
        [Tooltip("标签 Text 组件。也可用 Image 替代（将此留空，配置下方 Image）。")]
        [SerializeField] private Text _comboLabelText;

        [Tooltip("标签 Image 组件（用艺术字图片时配置此项）")]
        [SerializeField] private Image _comboLabelImage;

        [Header("倍率数字显示")]
        [Tooltip("使用 NumberImageDisplay 显示倍率纯数字（推荐，可配置数字精灵）")]
        [SerializeField] private NumberImageDisplay _multiplierNumberDisplay;

        [Tooltip("备用：使用 Text 组件显示倍率数字（NumberImageDisplay 为空时使用）")]
        [SerializeField] private Text _multiplierText;

        /// <summary>
        /// 设置 Combo 倍率。显示效果由 Prefab 中的组件样式决定。
        /// </summary>
        public void SetCombo(int multiplier)
        {
            if (_multiplierNumberDisplay != null)
            {
                _multiplierNumberDisplay.SetNumber(multiplier);
            }
            else if (_multiplierText != null)
            {
                _multiplierText.text = multiplier.ToString();
            }
        }

        /// <summary>
        /// 设置透明度（用于动画淡出）
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_comboLabelText != null)
            {
                var c = _comboLabelText.color;
                _comboLabelText.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (_comboLabelImage != null)
            {
                var c = _comboLabelImage.color;
                _comboLabelImage.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (_multiplierText != null)
            {
                var c = _multiplierText.color;
                _multiplierText.color = new Color(c.r, c.g, c.b, alpha);
            }
            // NumberImageDisplay 的子 Image 由 CanvasGroup 统一控制淡出，无需单独处理
        }
    }
}
