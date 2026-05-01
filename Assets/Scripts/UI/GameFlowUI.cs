using BlockPuzzle.Audio;
using BlockPuzzle.Core;
using BlockPuzzle.Save;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 传统模式游戏内流程 UI：返回 Title、设置弹窗、重新开始、继续游戏。
    /// 改为 Prefab 加载模式，Prefab 放在 Resources/Prefabs/UI/ 下。
    /// </summary>
    public class GameFlowUI : MonoBehaviour
    {
        private const string PrefabPath = "Prefabs/UI/";

        private GameObject _settingsPanel;
        private GameObject _dismissOverlay;
        private UserSettingsData _settings;

        // 设置面板控件引用
        private Toggle _musicToggle;
        private Slider _musicSlider;
        private Toggle _soundToggle;
        private Slider _soundSlider;
        private Image _vibrationToggleImage;
        private bool _vibrationOn;
        private Sprite _toggleOnSprite;
        private Sprite _toggleOffSprite;

        private void Awake()
        {
            LoadHudButtons();
            LoadSettingsPanel();
        }

        private void LoadHudButtons()
        {
            // 返回按钮
            var backPrefab = Resources.Load<GameObject>(PrefabPath + "BackToTitleButton");
            if (backPrefab != null)
            {
                var backGo = Instantiate(backPrefab, transform, false);
                backGo.name = "BackToTitleButton";
                var backBtn = backGo.GetComponent<Button>();
                if (backBtn != null)
                    backBtn.onClick.AddListener(() => GameManager.Instance.ReturnToTitle());
            }
            else
            {
                Debug.LogWarning("[GameFlowUI] BackToTitleButton Prefab 未找到，使用代码回退创建");
                var backButton = RuntimeUiFactory.CreateButton(transform, "BackToTitleButton", "返回", new Vector2(180f, 72f), new Color(0.26f, 0.28f, 0.36f, 0.92f));
                var backRect = backButton.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0f, 1f);
                backRect.anchorMax = new Vector2(0f, 1f);
                backRect.anchoredPosition = new Vector2(120f, -80f);
                backButton.onClick.AddListener(() => GameManager.Instance.ReturnToTitle());
            }

            // 设置按钮
            var settingsPrefab = Resources.Load<GameObject>(PrefabPath + "GameSettingsButton");
            if (settingsPrefab != null)
            {
                var settingsGo = Instantiate(settingsPrefab, transform, false);
                settingsGo.name = "GameSettingsButton";
                var settingsBtn = settingsGo.GetComponent<Button>();
                if (settingsBtn != null)
                    settingsBtn.onClick.AddListener(() => ShowSettings(true));
            }
            else
            {
                Debug.LogWarning("[GameFlowUI] GameSettingsButton Prefab 未找到，使用代码回退创建");
                var settingsButton = RuntimeUiFactory.CreateButton(transform, "GameSettingsButton", "设置", new Vector2(180f, 72f), new Color(0.26f, 0.28f, 0.36f, 0.92f));
                var settingsRect = settingsButton.GetComponent<RectTransform>();
                settingsRect.anchorMin = new Vector2(1f, 1f);
                settingsRect.anchorMax = new Vector2(1f, 1f);
                settingsRect.anchoredPosition = new Vector2(-120f, -80f);
                settingsButton.onClick.AddListener(() => ShowSettings(true));
            }
        }

        private void LoadSettingsPanel()
        {
            _settings = SaveManager.Instance.GetSettings();

            var panelPrefab = Resources.Load<GameObject>(PrefabPath + "GameSettingsPanel");
            if (panelPrefab != null)
            {
                _settingsPanel = Instantiate(panelPrefab, transform, false);
                _settingsPanel.name = "GameSettingsPanel";
                BindSettingsPanel();
            }
            else
            {
                Debug.LogWarning("[GameFlowUI] GameSettingsPanel Prefab 未找到，使用代码回退创建");
                BuildSettingsPanelFallback();
            }

            _settingsPanel.SetActive(false);
        }

        /// <summary>
        /// 绑定 Prefab 中的控件引用和事件
        /// </summary>
        private void BindSettingsPanel()
        {
            // 音乐 Toggle
            var musicToggleObj = _settingsPanel.transform.Find("MusicRow/MusicToggle");
            if (musicToggleObj != null)
            {
                _musicToggle = musicToggleObj.GetComponent<Toggle>();
                if (_musicToggle != null)
                {
                    _musicToggle.isOn = _settings.musicEnabled;
                    _musicToggle.onValueChanged.AddListener(v => { _settings.musicEnabled = v; SaveSettings(); });
                }
            }

            // 音乐音量滑块
            var musicSliderObj = _settingsPanel.transform.Find("MusicRow/MusicSlider");
            if (musicSliderObj != null)
            {
                _musicSlider = musicSliderObj.GetComponent<Slider>();
                if (_musicSlider != null)
                {
                    _musicSlider.value = _settings.musicVolume;
                    _musicSlider.onValueChanged.AddListener(v => { _settings.musicVolume = v; SaveSettings(); });
                }
            }

            // 音效 Toggle
            var soundToggleObj = _settingsPanel.transform.Find("SoundRow/SoundToggle");
            if (soundToggleObj != null)
            {
                _soundToggle = soundToggleObj.GetComponent<Toggle>();
                if (_soundToggle != null)
                {
                    _soundToggle.isOn = _settings.soundEnabled;
                    _soundToggle.onValueChanged.AddListener(v => { _settings.soundEnabled = v; SaveSettings(); });
                }
            }

            // 音效音量滑块
            var soundSliderObj = _settingsPanel.transform.Find("SoundRow/SoundSlider");
            if (soundSliderObj != null)
            {
                _soundSlider = soundSliderObj.GetComponent<Slider>();
                if (_soundSlider != null)
                {
                    _soundSlider.value = _settings.soundVolume;
                    _soundSlider.onValueChanged.AddListener(v => { _settings.soundVolume = v; SaveSettings(); });
                }
            }

            // 震动开关（图片按钮）
            var vibrationBtnObj = _settingsPanel.transform.Find("VibrationRow/VibrationToggleBtn");
            if (vibrationBtnObj != null)
            {
                _vibrationToggleImage = vibrationBtnObj.GetComponent<Image>();
                var vibBtn = vibrationBtnObj.GetComponent<Button>();
                _vibrationOn = _settings.vibrationEnabled;

                // 加载 toggle 图标
                _toggleOnSprite = Resources.Load<Sprite>("Art/UI/Buttons/btn_toggle_on");
                _toggleOffSprite = Resources.Load<Sprite>("Art/UI/Buttons/btn_toggle_off");

                UpdateVibrationIcon();
                if (vibBtn != null)
                    vibBtn.onClick.AddListener(ToggleVibration);
            }

            // 重新开始
            var restartBtn = _settingsPanel.transform.Find("RestartButton")?.GetComponent<Button>();
            if (restartBtn != null)
                restartBtn.onClick.AddListener(() => { ShowSettings(false); GameManager.Instance.RestartGame(); });

            // 返回 Title
            var titleBtn = _settingsPanel.transform.Find("ReturnTitleButton")?.GetComponent<Button>();
            if (titleBtn != null)
                titleBtn.onClick.AddListener(() => GameManager.Instance.ReturnToTitle());

            // 继续游戏
            var continueBtn = _settingsPanel.transform.Find("ContinueButton")?.GetComponent<Button>();
            if (continueBtn != null)
                continueBtn.onClick.AddListener(() => ShowSettings(false));
        }

        private void ToggleVibration()
        {
            _vibrationOn = !_vibrationOn;
            _settings.vibrationEnabled = _vibrationOn;
            SaveSettings();
            UpdateVibrationIcon();
        }

        private void UpdateVibrationIcon()
        {
            if (_vibrationToggleImage == null) return;
            _vibrationToggleImage.sprite = _vibrationOn ? _toggleOnSprite : _toggleOffSprite;
            if (_vibrationToggleImage.sprite != null)
                _vibrationToggleImage.SetNativeSize();
        }

        private void SaveSettings()
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveSettings(_settings);
        }

        private void ShowSettings(bool visible)
        {
            if (visible)
            {
                // 创建全屏遮罩（点击面板外部关闭）
                if (_dismissOverlay == null)
                {
                    _dismissOverlay = RuntimeUiFactory.CreateDismissOverlay(transform, "SettingsDismissOverlay", () => ShowSettings(false));
                }
                _dismissOverlay.SetActive(true);
                _dismissOverlay.transform.SetAsLastSibling();
                _settingsPanel.transform.SetAsLastSibling(); // 确保面板在遮罩之上
            }
            else
            {
                if (_dismissOverlay != null)
                    _dismissOverlay.SetActive(false);
            }

            _settingsPanel.SetActive(visible);
            if (visible)
                GameManager.Instance.PauseGame();
            else
                GameManager.Instance.ResumeGame();

            var audioManager = AudioManager.Current;
            if (audioManager != null)
                audioManager.PlayCue(visible ? AudioCueId.UiOpen : AudioCueId.UiClose);
        }

        // ==================== Fallback（Prefab 不存在时） ====================

        private void BuildSettingsPanelFallback()
        {
            _settingsPanel = RuntimeUiFactory.CreatePanel(transform, "GameSettingsPanel", new Color(0f, 0f, 0f, 0.84f), new Vector2(720f, 760f));

            var title = RuntimeUiFactory.CreateText(_settingsPanel.transform, "Title", "传统模式设置", 42, Color.white, TextAnchor.MiddleCenter);
            title.rectTransform.anchoredPosition = new Vector2(0f, 300f);

            var musicToggle = RuntimeUiFactory.CreateToggle(_settingsPanel.transform, "MusicToggle", "音乐", _settings.musicEnabled);
            musicToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30f, 205f);
            musicToggle.onValueChanged.AddListener(v => { _settings.musicEnabled = v; SaveSettings(); });
            var musicSlider = RuntimeUiFactory.CreateSlider(_settingsPanel.transform, "MusicVolume", _settings.musicVolume);
            musicSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(110f, 150f);
            musicSlider.onValueChanged.AddListener(v => { _settings.musicVolume = v; SaveSettings(); });

            var soundToggle = RuntimeUiFactory.CreateToggle(_settingsPanel.transform, "SoundToggle", "音效", _settings.soundEnabled);
            soundToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30f, 80f);
            soundToggle.onValueChanged.AddListener(v => { _settings.soundEnabled = v; SaveSettings(); });
            var soundSlider = RuntimeUiFactory.CreateSlider(_settingsPanel.transform, "SoundVolume", _settings.soundVolume);
            soundSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(110f, 25f);
            soundSlider.onValueChanged.AddListener(v => { _settings.soundVolume = v; SaveSettings(); });

            var vibrationToggle = RuntimeUiFactory.CreateToggle(_settingsPanel.transform, "VibrationToggle", "震动", _settings.vibrationEnabled);
            vibrationToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30f, -50f);
            vibrationToggle.onValueChanged.AddListener(v => { _settings.vibrationEnabled = v; SaveSettings(); });

            var restartButton = RuntimeUiFactory.CreateButton(_settingsPanel.transform, "RestartButton", "重新开始", new Vector2(360f, 76f), new Color(0.22f, 0.48f, 0.85f, 1f));
            restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -165f);
            restartButton.onClick.AddListener(() => { ShowSettings(false); GameManager.Instance.RestartGame(); });

            var titleButton = RuntimeUiFactory.CreateButton(_settingsPanel.transform, "ReturnTitleButton", "返回 Title", new Vector2(360f, 76f), new Color(0.35f, 0.35f, 0.45f, 1f));
            titleButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -255f);
            titleButton.onClick.AddListener(() => GameManager.Instance.ReturnToTitle());

            var continueButton = RuntimeUiFactory.CreateButton(_settingsPanel.transform, "ContinueButton", "继续游戏", new Vector2(360f, 76f), new Color(0.35f, 0.55f, 0.35f, 1f));
            continueButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -345f);
            continueButton.onClick.AddListener(() => ShowSettings(false));
        }
    }
}
