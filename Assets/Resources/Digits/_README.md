# 数字图集 (Digits)

## 这个目录放什么
分数显示和飘字用的数字 Sprite(0~9 共 10 张一组)。

## 文件命名
- `SH1_0.png` ~ `SH1_9.png` — 字体风格 1
- `SH2_0.png` ~ `SH2_9.png` — 字体风格 2(当前默认)
- 新增风格:`SH3_0.png` ~ `SH3_9.png` 等

## 推荐尺寸
每张数字 **96×128** 像素(高度比宽度大,数字一般竖向)。

## 如何切换字体风格
- HUD 分数显示:在 `ScoreDisplay.prefab` 的 `NumberImageDisplay` 组件 Inspector 里换 `_numberSprites` 数组
- 飘字字体:打开 `FloatingTextLibrary.asset`,模板里 `digitSprites` 字段拖入新一组 Sprite

## 替换流程
1. 直接覆盖原文件即可,无需进 Unity
2. 想换字体风格,只在 SO 里改引用(美术能做,不需要程序)
