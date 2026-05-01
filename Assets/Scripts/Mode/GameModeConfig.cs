using System;
using UnityEngine;

namespace BlockPuzzle.Mode
{
    [CreateAssetMenu(fileName = "GameModeConfig", menuName = "BlockPuzzle/Mode/Game Mode Config")]
    public class GameModeConfig : ScriptableObject
    {
        public const string TraditionalId = "traditional";
        public const string AdventureId = "adventure";

        [SerializeField] private string _modeId = TraditionalId;
        [SerializeField] private string _displayName = "传统模式";
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _placeholder;
        [SerializeField] private string _sceneName = "Boot";
        [SerializeField] private int _sortOrder;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        public string ModeId => _modeId;
        public string DisplayName => _displayName;
        public bool Enabled => _enabled;
        public bool Placeholder => _placeholder;
        public string SceneName => _sceneName;
        public int SortOrder => _sortOrder;
        public string Description => _description;

#if UNITY_EDITOR
        public void EditorSetValues(string modeId, string displayName, bool enabled, bool placeholder, string sceneName, int sortOrder, string description)
        {
            _modeId = modeId;
            _displayName = displayName;
            _enabled = enabled;
            _placeholder = placeholder;
            _sceneName = sceneName;
            _sortOrder = sortOrder;
            _description = description;
        }
#endif
    }

    [Serializable]
    public struct GameModeInfo
    {
        public string ModeId;
        public string DisplayName;
        public bool Enabled;
        public bool Placeholder;
        public string SceneName;
        public int SortOrder;
        public string Description;

        public GameModeInfo(string modeId, string displayName, bool enabled, bool placeholder, string sceneName, int sortOrder, string description)
        {
            ModeId = modeId;
            DisplayName = displayName;
            Enabled = enabled;
            Placeholder = placeholder;
            SceneName = sceneName;
            SortOrder = sortOrder;
            Description = description;
        }

        public GameModeInfo(GameModeConfig config)
        {
            ModeId = config != null ? config.ModeId : string.Empty;
            DisplayName = config != null ? config.DisplayName : string.Empty;
            Enabled = config != null && config.Enabled;
            Placeholder = config != null && config.Placeholder;
            SceneName = config != null ? config.SceneName : string.Empty;
            SortOrder = config != null ? config.SortOrder : 0;
            Description = config != null ? config.Description : string.Empty;
        }
    }
}
