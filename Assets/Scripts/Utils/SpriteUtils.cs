using UnityEngine;

namespace BlockPuzzle.Utils
{
    /// <summary>
    /// 运行时动态创建 Sprite 的工具类 + 美术资源加载
    /// </summary>
    public static class SpriteUtils
    {
        private static Sprite _whiteSquare;

        // 缓存的美术资源
        private static Sprite _blockSprite;
        private static Sprite _cellSprite;
        private static Sprite _bgSprite;

        /// <summary>
        /// 获取一个1x1白色正方形 Sprite（缓存复用，作为 fallback）
        /// </summary>
        public static Sprite WhiteSquare
        {
            get
            {
                if (_whiteSquare == null)
                    _whiteSquare = CreateSquareSprite(Color.white, 32);
                return _whiteSquare;
            }
        }

        /// <summary>
        /// 获取方块 Sprite（美术资源优先，fallback 到白色方块）
        /// </summary>
        public static Sprite BlockSprite
        {
            get
            {
                if (_blockSprite == null)
                    _blockSprite = LoadSprite("Art/Blocks/blk_base");
                return _blockSprite ?? WhiteSquare;
            }
        }

        /// <summary>
        /// 获取棋盘格子 Sprite（美术资源优先，fallback 到白色方块）
        /// </summary>
        public static Sprite CellSprite
        {
            get
            {
                if (_cellSprite == null)
                    _cellSprite = LoadSprite("Art/Board/brd_cell");
                return _cellSprite ?? WhiteSquare;
            }
        }

        /// <summary>
        /// 获取背景 Sprite
        /// </summary>
        public static Sprite BackgroundSprite
        {
            get
            {
                if (_bgSprite == null)
                    _bgSprite = LoadSprite("Art/Backgrounds/bg_game");
                return _bgSprite;
            }
        }

        /// <summary>
        /// 从 Resources 加载 Sprite，失败返回 null
        /// </summary>
        public static Sprite LoadSprite(string resourcePath)
        {
            return Resources.Load<Sprite>(resourcePath);
        }

        /// <summary>
        /// 创建一个纯色正方形 Sprite
        /// </summary>
        public static Sprite CreateSquareSprite(Color color, int size = 32)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 创建一个带边框的正方形 Sprite
        /// </summary>
        public static Sprite CreateBorderedSquareSprite(Color fillColor, Color borderColor, int size = 32, int borderWidth = 2)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x < borderWidth || x >= size - borderWidth
                                 || y < borderWidth || y >= size - borderWidth;
                    pixels[y * size + x] = isBorder ? borderColor : fillColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
