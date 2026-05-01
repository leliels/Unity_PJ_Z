using UnityEngine;

namespace BlockPuzzle.Mode
{
    [CreateAssetMenu(fileName = "ModeCatalog", menuName = "BlockPuzzle/Mode/Mode Catalog")]
    public class ModeCatalog : ScriptableObject
    {
        public const string ResourcesPath = "Configs/ModeCatalog";

        [SerializeField] private GameModeConfig[] _modes;

        public GameModeConfig[] Modes => _modes;

#if UNITY_EDITOR
        public void EditorSetModes(GameModeConfig[] modes)
        {
            _modes = modes;
        }
#endif
    }
}
