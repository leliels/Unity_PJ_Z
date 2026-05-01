using BlockPuzzle.Audio;
using BlockPuzzle.Mode;
using BlockPuzzle.Save;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BlockPuzzle.UI
{
    public class TitleUI : MonoBehaviour
    {
        private Text _messageText;
        private GameObject _settingsPanel;
        private GameObject _settingsDismissOverlay;
        private GameObject _confirmPanel;
        private UserSettingsData _settings;
        private Toggle _musicToggle;
        private Toggle _soundToggle;
        private Toggle _vibrationToggle;
        private Slider _musicSlider;
        private Slider _soundSlider;

        private void Awake()
        {
            EnsureServices();
            Build();
        }

        private void EnsureServices()
        {
            _ = SaveManager.Instance;
            _ = ModeManager.Instance;
            AudioManager.Instance.PlayTitleBgm();

            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<InputSystemUIInputModule>();
#else
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
        }

        private void Build()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            var bg = RuntimeUiFactory.CreatePanel(transform, "Background", new Color(0.08f, 0.07f, 0.11f, 1f), Vector2.zero);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var title = RuntimeUiFactory.CreateText(transform, "Title", "快乐消消乐", 72, new Color(1f, 0.9f, 0.45f, 1f), TextAnchor.MiddleCenter);
            title.rectTransform.anchoredPosition = new Vector2(0f, 620f);
            title.rectTransform.sizeDelta = new Vector2(820f, 120f);

            float y = 330f;
            foreach (var mode in ModeManager.Instance.GetModes())
            {
                var button = RuntimeUiFactory.CreateButton(transform, $"ModeButton_{mode.ModeId}", mode.DisplayName, new Vector2(520f, 108f), mode.Placeholder ? new Color(0.25f, 0.25f, 0.32f, 1f) : new Color(0.2f, 0.45f, 0.85f, 1f));
                button.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y);
                var captured = mode;
                button.onClick.AddListener(() => OnModeClicked(captured));
                y -= 140f;
            }

            var settingsButton = RuntimeUiFactory.CreateButton(transform, "SettingsButton", "设置", new Vector2(360f, 88f), new Color(0.35f, 0.35f, 0.45f, 1f));
            settingsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -250f);
            settingsButton.onClick.AddListener(() => ShowSettings(true));

            _messageText = RuntimeUiFactory.CreateText(transform, "MessageText", string.Empty, 30, new Color(1f, 0.95f, 0.8f, 1f), TextAnchor.MiddleCenter);
            _messageText.rectTransform.anchoredPosition = new Vector2(0f, -390f);
            _messageText.rectTransform.sizeDelta = new Vector2(800f, 80f);

            var version = RuntimeUiFactory.CreateText(transform, "Version", "v0.1 M3", 24, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter);
            version.rectTransform.anchoredPosition = new Vector2(0f, -820f);

            BuildSettingsPanel();
            BuildConfirmPanel();
        }

        private void OnModeClicked(GameModeInfo mode)
        {
            if (!ModeManager.Instance.TryEnterMode(mode.ModeId, out var message))
                ShowMessage(message);
        }

        private void BuildSettingsPanel()
        {
            _settings = SaveManager.Instance.GetSettings();
            _settingsPanel = RuntimeUiFactory.CreatePanel(transform, "TitleSettingsPanel", new Color(0f, 0f, 0f, 0.84f), new Vector2(720f, 720f));
            _settingsPanel.SetActive(false);

            var title = RuntimeUiFactory.CreateText(_settingsPanel.transform, "Title", "设置", 46, Color.white, TextAnchor.MiddleCenter);
            title.rectTransform.anchoredPosition = new Vector2(0f, 280f);

            _musicToggle = RuntimeUiFactory.CreateToggle(_settingsPanel.transform, "MusicToggle", "音乐", _settings.musicEnabled);
            _musicToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30f, 180f);
            _musicToggle.onValueChanged.AddListener(v => { _settings.musicEnabled = v; SaveManager.Instance.SaveSettings(_settings); });
            _musicSlider = RuntimeUiFactory.CreateSlider(_settingsPanel.transform, "MusicVolume", _settings.musicVolume);
            _musicSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(110f, 125f);
            _musicSlider.onValueChanged.AddListener(v => { _settings.musicVolume = v; SaveManager.Instance.SaveSettings(_settings); });

            _soundToggle = RuntimeUiFactory.CreateToggle(_settingsPanel.transform, "SoundToggle", "音效", _settings.soundEnabled);
            _soundToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30f, 55f);
            _soundToggle.onValueChanged.AddListener(v => { _settings.soundEnabled = v; SaveManager.Instance.SaveSettings(_settings); });
            _soundSlider = RuntimeUiFactory.CreateSlider(_settingsPanel.transform, "SoundVolume", _settings.soundVolume);
            _soundSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(110f, 0f);
            _soundSlider.onValueChanged.AddListener(v => { _settings.soundVolume = v; SaveManager.Instance.SaveSettings(_settings); });

            _vibrationToggle = RuntimeUiFactory.CreateToggle(_settingsPanel.transform, "VibrationToggle", "震动", _settings.vibrationEnabled);
            _vibrationToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30f, -75f);
            _vibrationToggle.onValueChanged.AddListener(v => { _settings.vibrationEnabled = v; SaveManager.Instance.SaveSettings(_settings); });

            var clearButton = RuntimeUiFactory.CreateButton(_settingsPanel.transform, "ClearDataButton", "清除用户数据", new Vector2(420f, 80f), new Color(0.72f, 0.25f, 0.25f, 1f));
            clearButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -220f);
            clearButton.onClick.AddListener(() => _confirmPanel.SetActive(true));

            var closeButton = RuntimeUiFactory.CreateButton(_settingsPanel.transform, "CloseButton", "关闭", new Vector2(300f, 76f), new Color(0.35f, 0.35f, 0.45f, 1f));
            closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -320f);
            closeButton.onClick.AddListener(() => ShowSettings(false));
        }

        private void BuildConfirmPanel()
        {
            _confirmPanel = RuntimeUiFactory.CreatePanel(transform, "ClearDataConfirmPanel", new Color(0f, 0f, 0f, 0.92f), new Vector2(680f, 360f));
            _confirmPanel.SetActive(false);
            var text = RuntimeUiFactory.CreateText(_confirmPanel.transform, "Text", "确定清除所有本地用户数据？", 34, Color.white, TextAnchor.MiddleCenter);
            text.rectTransform.anchoredPosition = new Vector2(0f, 80f);
            text.rectTransform.sizeDelta = new Vector2(620f, 100f);

            var yes = RuntimeUiFactory.CreateButton(_confirmPanel.transform, "ConfirmButton", "确定清除", new Vector2(260f, 72f), new Color(0.72f, 0.25f, 0.25f, 1f));
            yes.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150f, -80f);
            yes.onClick.AddListener(() =>
            {
                SaveManager.Instance.ClearAllUserData();
                _settings = SaveManager.Instance.GetSettings();
                RefreshSettingsControls();
                _confirmPanel.SetActive(false);
                ShowSettings(false);
                ShowMessage("用户数据已清除");
            });

            var no = RuntimeUiFactory.CreateButton(_confirmPanel.transform, "CancelButton", "取消", new Vector2(220f, 72f), new Color(0.35f, 0.35f, 0.45f, 1f));
            no.GetComponent<RectTransform>().anchoredPosition = new Vector2(160f, -80f);
            no.onClick.AddListener(() => _confirmPanel.SetActive(false));
        }

        private void ShowSettings(bool visible)
        {
            if (visible)
            {
                if (_settingsDismissOverlay == null)
                    _settingsDismissOverlay = RuntimeUiFactory.CreateDismissOverlay(transform, "SettingsDismissOverlay", () => ShowSettings(false));
                _settingsDismissOverlay.SetActive(true);
                _settingsDismissOverlay.transform.SetAsLastSibling();
                _settingsPanel.transform.SetAsLastSibling();
            }
            else
            {
                if (_settingsDismissOverlay != null)
                    _settingsDismissOverlay.SetActive(false);
            }

            _settingsPanel.SetActive(visible);
            var audioManager = AudioManager.Current;
            if (audioManager == null) return;
            audioManager.PlayCue(visible ? AudioCueId.UiOpen : AudioCueId.UiClose);
        }

        private void RefreshSettingsControls()
        {
            if (_settings == null) return;
            _musicToggle?.SetIsOnWithoutNotify(_settings.musicEnabled);
            _soundToggle?.SetIsOnWithoutNotify(_settings.soundEnabled);
            _vibrationToggle?.SetIsOnWithoutNotify(_settings.vibrationEnabled);
            _musicSlider?.SetValueWithoutNotify(_settings.musicVolume);
            _soundSlider?.SetValueWithoutNotify(_settings.soundVolume);
        }

        private void ShowMessage(string message)
        {
            if (_messageText != null)
                _messageText.text = message;
        }
    }
}
