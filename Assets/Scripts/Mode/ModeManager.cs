using System;
using System.Collections.Generic;
using System.Linq;
using BlockPuzzle.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Mode
{
    public class ModeManager : Singleton<ModeManager>
    {
        public event Action<GameModeInfo> OnModeChanged;

        [SerializeField] private ModeCatalog _modeCatalog;

        public string CurrentModeId { get; private set; } = GameModeConfig.TraditionalId;
        public GameModeInfo CurrentMode { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
            {
                DontDestroyOnLoad(gameObject);
                EnsureCatalog();
                CurrentMode = FindMode(CurrentModeId);
            }
        }

        public IReadOnlyList<GameModeInfo> GetModes()
        {
            EnsureCatalog();
            List<GameModeInfo> result = new List<GameModeInfo>();
            if (_modeCatalog != null && _modeCatalog.Modes != null)
            {
                foreach (var config in _modeCatalog.Modes)
                {
                    if (config != null)
                        result.Add(new GameModeInfo(config));
                }
            }

            if (result.Count == 0)
                result.AddRange(CreateFallbackModes());

            return result.OrderBy(m => m.SortOrder).ToArray();
        }

        public GameModeInfo FindMode(string modeId)
        {
            foreach (var mode in GetModes())
            {
                if (string.Equals(mode.ModeId, modeId, StringComparison.OrdinalIgnoreCase))
                    return mode;
            }
            return CreateFallbackModes()[0];
        }

        public bool TryEnterMode(string modeId, out string message)
        {
            var mode = FindMode(modeId);
            if (!mode.Enabled)
            {
                message = string.IsNullOrEmpty(mode.Description) ? "该模式暂未开放" : mode.Description;
                return false;
            }

            if (mode.Placeholder)
            {
                message = string.IsNullOrEmpty(mode.Description) ? "敬请期待" : mode.Description;
                return false;
            }

            if (string.IsNullOrWhiteSpace(mode.SceneName))
            {
                message = "模式未配置启动场景";
                return false;
            }

            CurrentModeId = string.IsNullOrWhiteSpace(mode.ModeId) ? GameModeConfig.TraditionalId : mode.ModeId;
            CurrentMode = mode;
            OnModeChanged?.Invoke(mode);
            SceneManager.LoadScene(mode.SceneName);
            message = string.Empty;
            return true;
        }

        public void ReturnToTitle()
        {
            SceneManager.LoadScene("Title");
        }

        private void EnsureCatalog()
        {
            if (_modeCatalog == null)
                _modeCatalog = Resources.Load<ModeCatalog>(ModeCatalog.ResourcesPath);
        }

        private static GameModeInfo[] CreateFallbackModes()
        {
            return new[]
            {
                new GameModeInfo(GameModeConfig.TraditionalId, "传统模式", true, false, "Boot", 0, "进入当前已实现玩法"),
                new GameModeInfo(GameModeConfig.AdventureId, "冒险模式", true, true, string.Empty, 10, "冒险模式敬请期待")
            };
        }
    }
}
