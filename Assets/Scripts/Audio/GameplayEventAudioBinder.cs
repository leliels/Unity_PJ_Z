using UnityEngine;
using BlockPuzzle.Audio;
using BlockPuzzle.Config;
using BlockPuzzle.Core;

namespace BlockPuzzle.Audio
{
    /// <summary>
    /// 事件音效绑定执行者(M-R5)。
    /// 监听 GameplayEvents,按 AudioBindings 配置调用 AudioManager 播音效。
    ///
    /// 旧 GameplayAudioBinder 保留兼容,但本 Binder 是新版"事件 → SO"驱动方案,推荐使用。
    /// </summary>
    public class GameplayEventAudioBinder : MonoBehaviour
    {
        private AudioBindings _bindings;
        private GameplayTuning _tuning;

        public void Init(AudioBindings bindings, GameplayTuning tuning)
        {
            _bindings = bindings;
            _tuning = tuning;
        }

        private void OnEnable() { GameplayEvents.OnEvent += HandleEvent; }
        private void OnDisable() { GameplayEvents.OnEvent -= HandleEvent; }

        private void HandleEvent(GameplayEventId id, GameplayEventArgs args)
        {
            if (_bindings == null) return;
            if (_tuning != null && !_tuning.EnableSfx) return;

            foreach (var b in _bindings.GetBindings(id))
            {
                if (b == null || b.cue == null) continue;
                float pitch = b.pitchRandomRange.x == b.pitchRandomRange.y
                    ? b.pitchRandomRange.x
                    : Random.Range(b.pitchRandomRange.x, b.pitchRandomRange.y);
                AudioManager.Instance?.PlayCue(b.cue, b.volume, -1f, pitch);
            }
        }
    }
}
