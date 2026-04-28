# SpriteAtlas 数字图集打包指南

> 本文档说明数字精灵图集的生成和使用方式。  
> 属于操作类使用说明文档。

## 概述

本项目使用 **SH1 系列（黄色）** 和 **SH2 系列（白色）** 数字图片作为积分显示的字体。
这 20 张图片已打包为 **Sprite Atlas** 以优化运行时性能（减少 Draw Call、节省内存）。

## 资源位置

| 内容 | 路径 |
|------|------|
| 原始图片 | `Assets/Art/拆分资源/` |
| Resources 运行时副本 | `Assets/Resources/Digits/` (SH1_0~9.png, SH2_0~9.png) |
| 图集文件 | `Assets/Art/DigitSpritesAtlas.spriteatlas` |

## 一键生成/重新生成图集

### 方法：Unity 菜单操作

1. 打开 Unity 编辑器
2. 顶部菜单栏 → **Tools** → **Create Digit Sprite Atlas**
3. 等待几秒，控制台会输出成功日志
4. 在 Project 窗口中自动选中生成的 `DigitSpritesAtlas.spriteatlas`

> 该功能由 `Assets/Scripts/Editor/CreateDigitAtlas.cs` 提供。

### 如果菜单没有出现

- 确认 `CreateDigitAtlas.cs` 位于 `Editor/` 文件夹下（或任何名为 Editor 的子目录）
- 右键 Project 窗口 → **Reimport All** 强制刷新脚本编译

## 手动创建图集（备选方案）

如果不想用脚本，也可以手动创建：

1. **Project 窗口右键** → **Create** → **2D** → **Sprite Atlas**
2. 命名为 `DigitSpritesAtlas`，放在 `Assets/Art/` 下
3. **Inspector 中**：
   - **Objects for Packing** 区域：将 `Resources/Digits/` 下全部 20 张拖入
   - （或直接拖入整个 `Digits` 文件夹）

## 后期重新生成

当美术更新了数字图片后：

1. 将新图片替换到 `Assets/Resources/Digits/` 目录
2. 执行 **Tools → Create Digit Sprite Atlas**
3. 图集自动重建，无需改代码

## 技术细节

### 图集配置参数

```csharp
packingSettings:
  enableRotation: false     // 数字图片不需要旋转
  enableTightPacking: true   // 紧密打包减少空白
  padding: 2                 // 图片间 2px 间距（防止边缘溢出）

textureSettings:
  generateMipMaps: false      // UI 不需要 MipMap
  filterMode: Bilinear       // 双线性过滤（缩放平滑）
  readable: false            // 运行时不需读取像素
```

### 运行时加载代码

```csharp
// 加载白色数字（当前积分）
var whiteDigits = Utils.SpriteUtils.SH2NumberSprites;

// 加载黄色数字（最高分）
var yellowDigits = Utils.SpriteUtils.SH1NumberSprites;
```

### 组件使用

```csharp
// NumberImageDisplay 自动按位渲染每个数字
var display = gameObject.AddComponent<NumberImageDisplay>();
display.SetNumberSprites(Utils.SpriteUtils.SH2NumberSprites);
display.SetNumber(12345); // 显示 "12345" 图片
```
