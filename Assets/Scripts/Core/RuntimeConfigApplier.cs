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

            var layout = config.Layout;

            // 1. BoardManager: 重新注入 layout/theme 并重摆格子
            var board = BoardManager.Current ?? BoardManager.Instance;
            if (board != null)
            {
                board.Configure(layout, config.Theme);
                // 重建 BoardLayout(让新的 CellSpacingRatio 等生效)
                board.RebuildLayout();
                board.RelayoutCells();
            }

            // 2. 棋盘位置偏移(BoardRoot anchoredPosition)
            var boardRoot = SceneBootstrap.BoardRoot;
            if (boardRoot != null && layout != null)
            {
                var canvas = boardRoot.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var canvasRt = (RectTransform)canvas.transform;
                    float screenW = canvasRt.rect.width;
                    float screenH = canvasRt.rect.height;
                    float offX = layout.BoardOffsetXRatio * screenW;
                    float offY = layout.BoardOffsetYRatio * screenH;
                    boardRoot.anchoredPosition = new Vector2(offX, offY);
                }
            }

            // 3. BlockSpawner: 重新注入,候选区不重建(避免方块掉数据)
            var spawner = BlockSpawner.Current ?? BlockSpawner.Instance;
            if (spawner != null)
            {
                spawner.Configure(layout, config.Theme);
                if (config.Shapes != null) spawner.SetShapeDatabase(config.Shapes);
            }

            Debug.Log("[RuntimeConfigApplier] 已重新应用 GameConfig。");
        }
    }
}
