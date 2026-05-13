# 方块切图 (Blocks)

## 这个目录放什么
拖拽方块的单格 Sprite。运行时 `BlockSpawner` 生成方块时使用。

## 文件命名
- `blk_base.png` — 基础方块(已有)
- 想要每色独立切图:`blk_red.png` / `blk_orange.png` / `blk_yellow.png` / `blk_green.png` / `blk_blue.png` / `blk_purple.png`

## 推荐尺寸
- **128×128** 像素(@1x = 设计稿对应像素,Unity 会按 RectTransform sizeDelta 缩放)
- 格式:PNG,带透明通道
- 描边、内高光建议在切图里画死,纯色由代码 tint

## 当前状态
只用 `blk_base.png` 一张图 + 代码 tint 6 色。可以选择性出独立切图(M-R5 之后 UIThemeConfig 加 `BlockSprites[6]` 数组,缺失则回退到纯色)。

## 替换流程
直接覆盖 PNG 文件,Unity 自动刷新。
