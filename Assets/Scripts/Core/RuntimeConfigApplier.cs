using UnityEngine;
using BlockPuzzle.Block;
using BlockPuzzle.Board;
using BlockPuzzle.Config;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// 运行时配置应用器:把当前 GameConfig / LayoutConfig / UIThemeConfig 的最新值
    /// 应用到正在运行中的 Manager,无需重启游戏。
    ///
    /// 调用方式:
    ///   1. 菜单 BlockPuzzle/重新应用游戏配置(Play 模式中)
    ///   2. 代码:RuntimeConfigApplier.ReapplyAll()
    ///   3. 任何 SO 字段在 Inspector 上调整,Unity 会触发 OnValidate,
    ///      ReapplyAll 也可以从 OnValidate 接力(目前不自动连接,以避免编辑期空跑)
    /// </summary>
    public static class RuntimeConfigApplier
    {
        /// <summary>把 GameConfig 当前的所有值重新应用一遍。</summary>
        public static void ReapplyAll()
        {
            var config = SceneBootstrap.ActiveConfig;
            if (config == null)
            {
                Debug.LogWarning("[RuntimeConfigApplier] 当前没有加载 GameConfig,无法重应用。");
                return;
            }

            // 1. BoardManager: 重新注入 layout/theme 并重摆格子
            var board = BoardManager.Current ?? BoardManager.Instance;
            if (board != null)
            {
                board.Configure(config.Layout, config.Theme);
                board.RelayoutCells();
            }

            // 2. BlockSpawner: 重新注入,候选区不重建(避免方块掉数据)
            var spawner = BlockSpawner.Current ?? BlockSpawner.Instance;
            if (spawner != null)
            {
                spawner.Configure(config.Layout, config.Theme);
                if (config.Shapes != null) spawner.SetShapeDatabase(config.Shapes);
            }

            Debug.Log("[RuntimeConfigApplier] 已重新应用 GameConfig(棋盘格已按 LayoutConfig/UIThemeConfig 重新摆放)。");
        }
    }
}
