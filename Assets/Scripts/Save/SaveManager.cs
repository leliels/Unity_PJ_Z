using System;
using System.Collections.Generic;
using BlockPuzzle.Core;
using BlockPuzzle.Mode;
using UnityEngine;

namespace BlockPuzzle.Save
{
    [Serializable]
    public class UserSettingsData
    {
        public bool musicEnabled = true;
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        [Range(0f, 1f)] public float soundVolume = 0.8f;
    }

    [Serializable]
    public class ModeSummaryData
    {
        public string modeId;
        public int highScore;
        public int playCount;
        public long totalScore;
        public string lastPlayTime;
    }

    [Serializable]
    public class PlayRecordData
    {
        public string modeId;
        public int score;
        public string startTime;
        public string endTime;
        public float durationSeconds;
    }

    [Serializable]
    public class PlayRecordList
    {
        public List<PlayRecordData> records = new List<PlayRecordData>();
    }

    public class SaveManager : Singleton<SaveManager>
    {
        private const string SettingsKey = "BlockPuzzle_Settings";
        private const string PlayRecordsKey = "BlockPuzzle_PlayRecords";
        private const int MaxRecordCount = 30;

        public event Action<UserSettingsData> OnSettingsChanged;
        public event Action OnDataCleared;

        private UserSettingsData _settings;

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
            {
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
        }

        public UserSettingsData GetSettings()
        {
            if (_settings == null)
                LoadSettings();
            return _settings;
        }

        public void SaveSettings(UserSettingsData settings)
        {
            _settings = settings ?? new UserSettingsData();
            PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(_settings));
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke(_settings);
        }

        public int GetHighScore(string modeId = null)
        {
            modeId = NormalizeModeId(modeId);
            return Mathf.Max(PlayerPrefs.GetInt(GetHighScoreKey(modeId), 0), PlayerPrefs.GetInt("BlockPuzzle_HighScore", 0));
        }

        public bool TryUpdateHighScore(string modeId, int score)
        {
            modeId = NormalizeModeId(modeId);
            int oldScore = GetHighScore(modeId);
            if (score <= oldScore) return false;
            PlayerPrefs.SetInt(GetHighScoreKey(modeId), score);
            PlayerPrefs.Save();
            return true;
        }

        public void RegisterPlayResult(string modeId, int score, DateTime startTime, DateTime endTime)
        {
            modeId = NormalizeModeId(modeId);
            TryUpdateHighScore(modeId, score);

            var summary = LoadModeSummary(modeId);
            summary.modeId = modeId;
            summary.highScore = GetHighScore(modeId);
            summary.playCount++;
            summary.totalScore += Mathf.Max(0, score);
            summary.lastPlayTime = endTime.ToString("O");
            PlayerPrefs.SetString(GetSummaryKey(modeId), JsonUtility.ToJson(summary));

            var list = LoadPlayRecords();
            list.records.Insert(0, new PlayRecordData
            {
                modeId = modeId,
                score = score,
                startTime = startTime.ToString("O"),
                endTime = endTime.ToString("O"),
                durationSeconds = Mathf.Max(0f, (float)(endTime - startTime).TotalSeconds)
            });
            while (list.records.Count > MaxRecordCount)
                list.records.RemoveAt(list.records.Count - 1);
            PlayerPrefs.SetString(PlayRecordsKey, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
        }

        public void ClearAllUserData()
        {
            PlayerPrefs.DeleteKey(SettingsKey);
            PlayerPrefs.DeleteKey(PlayRecordsKey);
            PlayerPrefs.DeleteKey("BlockPuzzle_HighScore");

            DeleteModeData(GameModeConfig.TraditionalId);
            DeleteModeData(GameModeConfig.AdventureId);

            if (ModeManager.Current != null)
            {
                foreach (var mode in ModeManager.Current.GetModes())
                    DeleteModeData(mode.ModeId);
            }

            PlayerPrefs.Save();
            _settings = new UserSettingsData();
            OnSettingsChanged?.Invoke(_settings);
            OnDataCleared?.Invoke();
        }

        private static void DeleteModeData(string modeId)
        {
            modeId = NormalizeModeId(modeId);
            PlayerPrefs.DeleteKey(GetHighScoreKey(modeId));
            PlayerPrefs.DeleteKey(GetSummaryKey(modeId));
        }

        private void LoadSettings()
        {
            string json = PlayerPrefs.GetString(SettingsKey, string.Empty);
            _settings = string.IsNullOrEmpty(json) ? new UserSettingsData() : JsonUtility.FromJson<UserSettingsData>(json);
            if (_settings == null)
                _settings = new UserSettingsData();
        }

        private ModeSummaryData LoadModeSummary(string modeId)
        {
            string json = PlayerPrefs.GetString(GetSummaryKey(modeId), string.Empty);
            var summary = string.IsNullOrEmpty(json) ? new ModeSummaryData() : JsonUtility.FromJson<ModeSummaryData>(json);
            if (summary == null)
                summary = new ModeSummaryData();
            summary.modeId = modeId;
            summary.highScore = GetHighScore(modeId);
            return summary;
        }

        private PlayRecordList LoadPlayRecords()
        {
            string json = PlayerPrefs.GetString(PlayRecordsKey, string.Empty);
            var list = string.IsNullOrEmpty(json) ? new PlayRecordList() : JsonUtility.FromJson<PlayRecordList>(json);
            return list ?? new PlayRecordList();
        }

        private static string NormalizeModeId(string modeId)
        {
            return string.IsNullOrWhiteSpace(modeId) ? GameModeConfig.TraditionalId : modeId.Trim().ToLowerInvariant();
        }

        private static string GetHighScoreKey(string modeId) => $"BlockPuzzle_Mode_{modeId}_HighScore";
        private static string GetSummaryKey(string modeId) => $"BlockPuzzle_Mode_{modeId}_Summary";
    }
}
