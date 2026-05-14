using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Audio;
using BlockPuzzle.Core;

namespace BlockPuzzle.Config
{
    /// <summary>
    /// 事件音效绑定:把"游戏事件"映射到"播哪个 AudioCue"。
    /// AudioLibrary 还是用作"音效素材库",这里负责"事件 → 素材"的对应关系。
    ///
    /// 美术换音效流程:
    ///   1. 把新音频拖进项目,做成 AudioCue.asset(菜单 BlockPuzzle/Audio/Audio Cue)
    ///   2. 打开 AudioBindings.asset,把对应事件那条的 cue 字段换成新 AudioCue
    ///   3. 完成
    /// </summary>
    [CreateAssetMenu(fileName = "AudioBindings", menuName = "BlockPuzzle/游戏配置/事件音效绑定")]
    public sealed class AudioBindings : ScriptableObject
    {
        public const string ResourcesPath = "Configs/02_Feel/AudioBindings";

        [Serializable]
        public class Binding
        {
            [Tooltip("此条绑定对应的游戏事件。")]
            public GameplayEventId eventId;

            [Tooltip("事件触发时播放的 AudioCue。从 AudioLibrary 中选。")]
            public AudioCue cue;

            [Tooltip("音量倍率。"), Range(0f, 2f)]
            public float volume = 1f;

            [Tooltip("音调随机范围(min,max)。例如 (0.95, 1.05) 让每次播放略微不同。")]
            public Vector2 pitchRandomRange = new Vector2(1f, 1f);
        }

        [Tooltip("绑定列表。同一事件可挂多条,会同时播。")]
        [SerializeField] private List<Binding> _bindings = new List<Binding>();

        public IReadOnlyList<Binding> Bindings => _bindings;

        public IEnumerable<Binding> GetBindings(GameplayEventId id)
        {
            foreach (var b in _bindings)
                if (b != null && b.eventId == id) yield return b;
        }
    }
}
