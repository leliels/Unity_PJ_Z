using UnityEditor;
using UnityEngine;
using BlockPuzzle.Core;

namespace BlockPuzzle.EditorTools
{
    public static class ReapplyConfigMenu
    {
        [MenuItem("BlockPuzzle/重新应用游戏配置 (Play 模式中调试用) %#r")]
        public static void Reapply()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[重新应用游戏配置] 不在 Play 模式,改完配置启动游戏即可生效。");
                return;
            }
            RuntimeConfigApplier.ReapplyAll();
        }
    }
}
