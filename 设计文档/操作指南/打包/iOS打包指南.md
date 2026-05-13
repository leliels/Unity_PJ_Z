# iOS 打包指南

> 本文档说明如何将本项目打包为 iOS 应用。  
> 属于操作类使用说明文档。

## 概述

本项目基于 Unity 6 (6000.3.8f1) + URP 2D，竖屏 1080×1920。  
iOS 打包分为两个阶段：

1. **Windows 上**：在 Unity 中构建，生成 Xcode 工程（完成 90% 的工作）
2. **Mac 上**：用 Xcode 编译、签名、安装到设备或上架

> ⚠️ **Apple 限制**：Xcode 编译和代码签名**只能在 macOS 上完成**，Windows 无法替代。

---

## 前置条件清单

在开始之前，请确认以下条件是否满足：

### Windows 侧（构建 Xcode 工程）

| 条件 | 是否必须 | 说明 |
|------|:--------:|------|
| Unity 6 已安装 | ✅ 必须 | 当前版本 6000.3.8f1 |
| iOS Build Support 模块 | ✅ 必须 | 通过 Unity Hub 安装，见下文 |
| Apple 开发者账号 | ❌ 不需要 | Windows 阶段不涉及签名 |

### Mac 侧（编译签名出包）

| 条件 | 是否必须 | 说明 |
|------|:--------:|------|
| macOS 电脑 | ✅ 必须 | MacBook / iMac / Mac mini 均可 |
| Xcode（建议 15+） | ✅ 必须 | 从 Mac App Store 免费下载 |
| Apple ID | ✅ 必须 | 免费 Apple ID 即可用于真机调试 |
| Apple Developer Program | ⚠️ 视情况 | 见下方「开发者账号详解」 |
| iOS 真机 | ⚠️ 推荐 | 模拟器可以测试，但建议真机验证 |

---

## 开发者账号详解

这是最常见的疑问，这里说清楚：

### 免费 Apple ID（不花钱）

- **能做什么**：
  - ✅ 在 Xcode 中编译项目
  - ✅ 安装到**自己的 iPhone/iPad** 上进行调试（最多 3 台设备）
  - ✅ 使用 Xcode 模拟器测试
- **限制**：
  - ❌ 无法上架 App Store
  - ❌ 签名有效期只有 **7 天**，到期后需要重新编译安装
  - ❌ 不支持推送通知、App Groups 等高级能力（本项目暂不需要）
  - ❌ 无法使用 TestFlight 分发给测试用户

### Apple Developer Program（688 元/年）

- **能做什么**：
  - ✅ 免费 Apple ID 的所有能力
  - ✅ 上架 App Store
  - ✅ 使用 TestFlight 分发（最多 10000 名外部测试用户）
  - ✅ 签名有效期 **1 年**
  - ✅ 安装设备数量上限 100 台
- **何时需要**：
  - 打算上架 App Store 时
  - 需要分发给多人测试时

### 建议

> **开发调试阶段用免费 Apple ID 就够了**，等到确定要上架时再购买开发者账号。

---

## 第一阶段：Windows 上构建 Xcode 工程

### 步骤 1：安装 iOS Build Support 模块

1. 打开 **Unity Hub**
2. 左侧点击 **Installs**
3. 找到当前使用的 Unity 6 版本 → 点击右侧 **齿轮图标** → **Add Modules**
4. 勾选 **iOS Build Support**
5. 点击 **Install**，等待下载完成（约 2~3 GB）

> 安装完成后重启 Unity 编辑器。

### 步骤 2：切换目标平台为 iOS

1. 打开项目，菜单栏 → **File → Build Settings**（快捷键 `Ctrl+Shift+B`）
2. 左侧平台列表中选择 **iOS**
3. 点击右下角 **Switch Platform**
4. 等待资源重新导入（首次切换可能需要几分钟到十几分钟）

> 切换完成后，左侧 iOS 旁会出现 Unity 图标，表示当前目标平台已切换。

### 步骤 3：配置 Player Settings

菜单栏 → **Edit → Project Settings → Player** → 选择 **iOS 标签页**（iPhone 图标）：

#### 基本信息

| 设置项 | 位置 | 建议值 | 说明 |
|--------|------|--------|------|
| Company Name | Player 顶部 | 你的团队名 | |
| Product Name | Player 顶部 | `快乐消消乐`（或正式名称） | 手机上显示的 App 名 |
| Bundle Identifier | Other Settings | `com.yourteam.blockpuzzle` | **必填**，全球唯一标识，建议用反向域名格式 |
| Version | Other Settings | `1.0.0` | 版本号 |
| Build | Other Settings | `1` | 每次提交 App Store 递增 |

#### 屏幕与方向

| 设置项 | 位置 | 建议值 | 说明 |
|--------|------|--------|------|
| Default Orientation | Resolution and Presentation | **Portrait** | 本项目为竖屏 |
| Allowed Orientations for Auto Rotation | 同上 | 只勾选 Portrait | 锁定竖屏 |

#### 兼容性

| 设置项 | 位置 | 建议值 | 说明 |
|--------|------|--------|------|
| Target minimum iOS Version | Other Settings | `15.0` | iOS 15 覆盖绝大多数在用设备 |
| Scripting Backend | Other Settings | IL2CPP（默认，不可更改） | iOS 只支持 IL2CPP |
| Target Architecture | Other Settings | ARM64（默认） | 所有现代 iOS 设备 |
| Api Compatibility Level | Other Settings | `.NET Standard 2.1` | 保持默认即可 |

#### 可以忽略的选项

| 设置项 | 说明 |
|--------|------|
| Require ARKit support | **取消勾选**，本项目不需要 AR |
| Camera Usage Description | 留空，本项目不使用摄像头 |
| Microphone Usage Description | 留空，本项目不使用麦克风 |
| Location Usage Description | 留空，本项目不使用定位 |

> ⚠️ 如果填写了 Usage Description 但实际没有使用对应硬件，Apple 审核可能会拒绝。不用的就留空。

### 步骤 4：配置图标（可选，后续再做也行）

**Player Settings → iOS → Icon**：

- 需要提供多种尺寸的 App 图标（180×180、167×167、152×152、120×120、76×76 等）
- 建议准备一张 **1024×1024** 的原图，Unity 会自动缩放生成各尺寸
- 如果暂时不配，系统会使用 Unity 默认图标

### 步骤 5：构建 Xcode 工程

1. **File → Build Settings**
2. 确认 **Scenes In Build** 中包含了 `Scenes/Boot`（如果列表为空，点击 **Add Open Scenes**）
3. 点击 **Build**
4. 选择一个**空文件夹**作为输出目录（例如在项目根目录旁创建 `iOS_Build/`）
5. 等待构建完成（首次构建可能需要 10~30 分钟，取决于电脑性能）

> 构建完成后，输出目录中会包含：
> ```
> iOS_Build/
> ├── Unity-iPhone.xcodeproj    ← Xcode 工程文件
> ├── Classes/                  ← IL2CPP 生成的 C++ 代码
> ├── Data/                     ← 游戏资源
> ├── Libraries/                ← Unity 运行时库
> ├── LaunchScreen-iPhone.png
> └── ...
> ```

### 步骤 6：传输到 Mac

将整个输出文件夹传输到 Mac，常见方式：

- **U 盘 / 移动硬盘**：最简单直接
- **局域网共享**：Windows 共享文件夹，Mac 通过 Finder → 连接服务器访问
- **云盘**：百度网盘、iCloud、Google Drive 等
- **Git**：把 Xcode 工程推送到仓库（注意文件较大，建议用 `.gitignore` 排除不必要文件）

> 💡 文件夹大小通常在 500MB ~ 2GB 之间。

---

## 第二阶段：Mac 上编译签名

### 步骤 1：安装 Xcode

1. 打开 Mac 上的 **App Store**
2. 搜索 **Xcode**，下载安装（约 12GB，需要较长时间）
3. 首次启动 Xcode 时，同意许可协议，等待安装附加组件

### 步骤 2：用 Xcode 打开工程

1. 找到传输过来的构建文件夹
2. 双击 `Unity-iPhone.xcodeproj` 打开（如果有 `.xcworkspace` 文件则优先打开它）

### 步骤 3：配置签名

1. 在 Xcode 左侧项目导航器中，点击顶层的 **Unity-iPhone** 项目
2. 选择 **TARGETS → Unity-iPhone**
3. 切换到 **Signing & Capabilities** 标签页
4. 勾选 **✅ Automatically manage signing**
5. **Team** 下拉框中选择你的 Apple ID（如果没有，点击 **Add an Account...** 登录）
6. 如果出现红色错误提示，通常是 Bundle Identifier 冲突，修改为唯一的值即可

> 同时对 **UnityFramework** Target 也执行相同的签名配置（选择同一个 Team）。

### 步骤 4：选择运行目标

- **真机**：用 USB 线连接 iPhone → 在 Xcode 顶部设备栏选择你的设备
  - 首次连接时，iPhone 上需要点击「信任此电脑」
  - 如果是免费账号，还需要在 iPhone 上：**设置 → 通用 → VPN 与设备管理 → 信任开发者证书**
- **模拟器**：在设备栏选择一个 iPhone 模拟器型号（如 iPhone 15 Pro）

### 步骤 5：编译运行

1. 点击 Xcode 左上角的 **▶ 按钮**（或 `Cmd + R`）
2. 等待编译（首次约 5~15 分钟）
3. 编译成功后会自动安装到设备/模拟器并启动

### 常见编译问题及解决

| 问题 | 原因 | 解决方法 |
|------|------|----------|
| `Signing for "Unity-iPhone" requires a development team` | 未配置签名 | 步骤 3 中选择 Team |
| `No profiles for 'com.xxx.xxx' were found` | Bundle ID 未注册 | 勾选 Automatically manage signing，Xcode 会自动处理 |
| `A build only device cannot be used to run this target` | iOS 版本过高/过低 | 更新 Xcode 或调整 Target minimum iOS Version |
| `UnityFramework` 签名错误 | UnityFramework 也需要签名 | 对 UnityFramework Target 也配置 Team |
| `Code Sign error: No matching provisioning profiles found` | 免费账号 7 天过期 | 重新编译即可刷新签名 |

---

## 导出 IPA 安装包（上架或分发用）

> 此步骤需要 **Apple Developer Program（付费账号）**。

1. 在 Xcode 中：**Product → Archive**
2. 等待 Archive 完成，自动弹出 **Organizer** 窗口
3. 选择刚生成的 Archive → 点击 **Distribute App**
4. 选择分发方式：

| 方式 | 说明 | 需要付费账号 |
|------|------|:---:|
| App Store Connect | 上传到 App Store / TestFlight | ✅ |
| Ad Hoc | 导出 .ipa 安装到指定设备（最多 100 台） | ✅ |
| Development | 导出开发用 .ipa | ✅ |
| Enterprise | 企业内部分发 | 需企业账号 |

---

## 本项目特别注意事项

### 1. 触屏输入兼容

本项目使用 New Input System。如果拖拽方块的逻辑使用了 `Pointer` 类型的 Action 绑定，iOS 触屏会自动兼容，无需额外修改。

如果代码中直接使用了 `Mouse.current`，则需要改为 `Pointer.current` 或同时处理 `Touchscreen.current`。

### 2. 安全区域适配（刘海屏 / 灵动岛）

iPhone 存在刘海和灵动岛区域，UI 需要避开这些区域。可以通过 `Screen.safeArea` 获取安全区域：

```csharp
// 获取安全区域（像素坐标）
Rect safeArea = Screen.safeArea;
```

> 建议在 M3 或更后面的阶段处理此问题，当前核心玩法验证阶段可以暂时忽略。

### 3. PlayerPrefs

`PlayerPrefs` 在 iOS 上存储在 App 沙盒的 `Library/Preferences/` 中，功能和 PC 上一致，无需修改。

### 4. Resources 目录

当前项目使用 `Resources/` 目录加载资源，iOS 打包时该目录下**所有内容**都会被打入包中。如果后续包体过大，可考虑迁移到 Addressables 按需加载。

### 5. URP 2D 性能

URP 2D 在 iOS 上性能良好。本项目是 2D 消除游戏，渲染压力很小，不需要额外的性能优化配置。

---

## 没有 Mac 的替代方案

| 方案 | 费用 | 说明 |
|------|------|------|
| **借一台 Mac** | 免费 | 只需要最后 Xcode 编译步骤，借用 30 分钟足够 |
| **Unity Cloud Build** | Unity 订阅内含 / 额外付费 | 上传项目到 Unity 云端自动构建，不需要 Mac |
| **租用云 Mac** | 约 $1/小时起 | MacStadium、AWS EC2 Mac 等服务 |
| **GitHub Actions + macOS Runner** | 免费额度有限 | 配置 CI/CD 自动构建（需要一定配置经验） |
| **macOS 虚拟机** | 免费（灰色地带） | VMware / VirtualBox 安装黑苹果，不稳定，不推荐 |

---

## 快速决策流程图

```
你想做什么？
│
├─ 只是想在自己 iPhone 上试玩
│   → 免费 Apple ID + 借一台 Mac + Xcode 编译
│   → 成本：0 元（签名 7 天有效，到期重新编译）
│
├─ 给几个朋友试玩测试
│   → 购买 Apple Developer Program（688 元/年）
│   → 使用 TestFlight 分发，最方便
│
└─ 上架 App Store
    → 购买 Apple Developer Program（688 元/年）
    → 准备 App Store 审核材料（截图、描述、隐私政策等）
```

---

## 附录：完整操作检查清单

- [ ] Unity Hub 中安装了 iOS Build Support 模块
- [ ] Unity 中已 Switch Platform 到 iOS
- [ ] 配置了 Bundle Identifier（`com.xxx.xxx` 格式）
- [ ] Default Orientation 设为 Portrait
- [ ] Target minimum iOS Version 设为 15.0
- [ ] Scenes In Build 中包含 `Scenes/Boot`
- [ ] Build 成功生成了 Xcode 工程文件夹
- [ ] 将文件夹传输到 Mac
- [ ] Mac 上安装了 Xcode
- [ ] Xcode 中配置了 Signing（Unity-iPhone 和 UnityFramework 两个 Target）
- [ ] 编译运行成功
