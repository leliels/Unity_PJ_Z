using UnityEngine;

namespace BlockPuzzle.Utils
{
    /// <summary>
    /// 运行时动态创建 Sprite 的工具类
    /// </summary>
    public static class SpriteUtils
    {
        private static Sprite _whiteSquare;

        /// <summary>
        /// 获取一个1x1白色正方形 Sprite（缓存复用）
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
        /// 创建一个纯色正方形 Sprite
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="size">纹理像素大小</param>
        /// <returns>生成的 Sprite</returns>
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
