# 快乐消消乐（暂定名）— 项目索引

> 本文件是项目的唯一入口。CodeBuddy 每次新对话会自动读取本文件。  
> 新成员也可以从这里快速了解项目全貌。

## 一句话介绍

2D 休闲方块消除游戏（类似《快乐爱消除》），Unity 6 + URP 2D，竖屏 1080×1920，8×8 棋盘。

## 当前进度

| 里程碑 | 状态 | 说明 |
|--------|------|------|
| M0 项目准备 | ✅ | 文档、目录、美术效果图 |
| M1 核心原型 | ✅ | 棋盘+方块+拖拽+消除+计分，已验收 |
| M2 数值体验+换皮 | ✅ | 计分、Prefab 化、美术替换与配置工具 |
| **2.0 重构** | ✅ | 配置中心 + 全 Canvas 化 + iOS 多机型适配 + 美术/策划自助体系(M-R1~R7,2026.5.13 完成) |
| M3 完整玩法 | 🔵 | UI流程+游戏模式+存档+音效(占位已搭,在 2.0 分支基础上推进) |
| M4 内容填充 | ⬜ | 特效+音效+冒险模式机制 |
| M5 打磨上线 | ⬜ | Bug修复+适配+上架 |

## 技术栈

- Unity 6 (6000.3.8f1)，Universal 2D 模板，URP 17.3.0
- C#，New Input System 1.18.0，uGUI
- Git 版本控制，不依赖第三方插件（不用 DOTween）

## 关键技术决策

- 竖屏 1080×1920 | 8×8 棋盘 | 3 候选方块用完刷新
- 消除后不下落 | 不支持玩家旋转 | 方块形状由配置工具维护，方向变体作为独立形状随机出现
- 计分采用 2026-04-30 配置化公式：仅消除计分，排分倍率 + Combo 数 + Combo CD 机制
- 数据存储 PlayerPrefs | 先做 PC 再移动端
- **2.0 重构后渲染统一 UGUI**(背景/棋盘/候选/拖拽/HUD 全部 Canvas + Image),不再用 SpriteRenderer。坐标换算走 `BoardLayout.ScreenToCell`,iOS 多机型自适配。
- **配置中心化**:`Assets/Resources/Configs/GameConfig.asset` 是唯一总入口,内部引用 11 份子 SO。代码不再有"棋盘格大小""候选区位置"等硬编码。
- **美术/策划自助**:特效/震屏/飘字/音效全部由 SO 配置驱动(FxLibrary / FloatingTextLibrary / AudioBindings),改这些不进代码。

## 项目目录结构

```
Unity_PJ_Z/
├── CLAUDE.md                  ← 你正在读的文件（项目索引）
├── README.md                  # Git 仓库首页（极简）
├── Assets/
│   ├── _美术与策划速查.md       ← **【美术/策划入口】1 屏速查,改 X 该改哪个 .asset**
│   ├── Scripts/               # C# 脚本（按模块分目录）
│   │   ├── Config/            # **2.0 新增:配置 SO 类型(GameConfig/LayoutConfig 等)**
│   │   ├── Core/              # 核心启动、状态、GameplayEvents 事件总线
│   │   ├── Board/             # 棋盘、UGUI 渲染、BoardLayout 坐标工具
│   │   ├── Block/             # 方块数据、UGUI 候选区、UGUI 拖拽
│   │   ├── Score/             # 计分配置、计算与状态管理
│   │   ├── UI/                # HUD、分数显示、飘字、SafeAreaFitter
│   │   ├── Audio/             # 音效系统、AudioBindings 事件绑定
│   │   ├── Feedback/          # FxManager 特效与震屏
│   │   ├── Mode/Save/         # 模式系统、存档
│   │   ├── Utils/             # 通用工具
│   │   └── Editor/            # 编辑器工具(BlockPuzzle 菜单全在这)
│   ├── Scenes/
│   │   ├── Title.unity        # Title 启动场景(Build index 0)
│   │   └── Boot.unity         # 对局场景(Build index 1,挂 SceneBootstrap)
│   ├── Resources/
│   │   ├── Art/               # 运行时美术资源,各子目录有 _README.md
│   │   ├── Configs/           # **配置 .asset 全部在这里,GameConfig.asset 是入口**
│   │   └── Digits/            # 数字精灵图（SH1/SH2 系列）
│   ├── Configs/               # 编辑器维护的配置(BlockShapeDatabase 备份)
│   ├── Art/拆分资源/           # 美术原始切图（效果图 + UI 素材）
│   ├── Prefabs/               # 预制体(2.0 重构:Block/Board/UI 等)
│   └── Settings/              # URP 渲染设置
├── Packages/                  # Unity 包管理
├── ProjectSettings/           # 项目设置
└── 设计文档/                   # 设计文档目录（见下方索引）
    ├── 会议内容/               # 开发人员自己用的一些文档,ai 不用看
    └── 操作指南/               # 操作类使用说明文档
```

## 配置中心入口（重要）

- **菜单 `BlockPuzzle/游戏配置中心`**:打开 GameConfig.asset,横排展示所有子配置 + "打开"按钮直达
- **`Assets/_美术与策划速查.md`**:美术/策划同学的 1 屏指引,200 字看完
- **菜单 `BlockPuzzle/AI 工具/创建自助体系配置 (FxLibrary 等)`**:一键生成 M-R5 的 3 份空白 SO

## 设计文档索引

> 详细内容请查阅对应文档，此处仅做索引。

| 文档 | 定位 | 何时需要读 |
|------|------|-----------|
| @设计文档/正式文档/01-项目总览.md | 团队分工、技术环境、已安装的 Unity 包 | 了解团队和技术环境 |
| @设计文档/正式文档/02-游戏设计文档.md | 核心玩法规则、消除机制、计分公式、模式系统、游戏流程 | 实现或修改玩法逻辑 |
| @设计文档/正式文档/03-技术架构文档.md | 脚本架构、目录规划、代码规范、Manager 系统设计 | 写代码前必读 |
| @设计文档/正式文档/04-UI界面规划.md | 所有界面布局、元素清单 | 实现 UI 时 |
| @设计文档/正式文档/05-美术资源规范.md | 命名规范、尺寸要求、素材清单、特效/音效需求 | 制作或接入美术资源 |
| @设计文档/正式文档/06-开发计划.md | 里程碑任务清单、当前待办、开发日志 | 了解进度或接续开发 |
| @设计文档/正式文档/07-待确认问题清单.md | 已确认和未确认的设计决策 | 遇到不确定的设计问题 |
| @设计文档/正式文档/08-美术资源替换指南.md | Resources 目录结构、资源映射表、替换流程 | 替换或接入美术资源 |
| @设计文档/正式文档/09-游戏内术语表.md | 游戏内对象、规则、界面、资源与代码术语统一 | 统一命名或避免术语混用 |

**操作指南**（使用说明类文档，非设计文档）：

| 文档 | 说明 |
|------|------|
| @设计文档/操作指南/SpriteAtlas图集打包指南.md | 数字图集的生成和使用方式 |
| @设计文档/操作指南/方块形状配置工具使用指南.md | 方块形状配置工具的设计说明与使用流程 |
| @设计文档/操作指南/M3音效与反馈配置指南.md | M3 音效、按钮触发、玩法事件音效和震动/抖动反馈配置 |
| @设计文档/操作指南/iOS打包指南.md | iOS 打包完整流程（Windows 构建 + Mac 签名）、开发者账号说明 |

## 架构要点

- **Title.unity**(Build index 0)→ **Boot.unity**(对局场景,Build index 1)。开发者也可直接打 Boot 进游戏。
- **SceneBootstrap**(Boot 场景唯一挂载脚本)负责启动一切:加载 GameConfig → 搭 4 Canvas → 创建 Manager → 注入配置
- **4 Canvas 层**:BackgroundCanvas / PlayCanvas / HudCanvas / OverlayCanvas,统一 ScaleWithScreenSize match=0.5
- **棋盘渲染**:UGUI Image,坐标走 `BoardLayout.ScreenToCell`,屏幕宽高比变 → 棋盘自动等比缩放
- **Manager 单例**:GameManager → BoardManager / BlockSpawner / ScoreManager / GameUI / FxManager / FloatingTextManager / GameplayEventAudioBinder
- **通信方式**:`GameplayEvents` 静态事件总线 + 各 Manager 自身的 C# event(原有 OnLineCleared 等保留兼容)
- **代码中保留 fallback**:Prefab/资源为 null 时走代码生成路径,确保不崩溃

## 开发规则

- **CODEBUDDY.md 是项目索引，不是开发日志**：只有项目定位、里程碑、关键技术决策、顶层目录、文档索引或核心架构发生变化时才更新；普通新增脚本、Prefab、资源或小逻辑修改不要改本文件。
- **文档批量同步**：功能开发过程中优先完成代码与 Unity 配置；一个完整功能/阶段完成并验证后，再一次性更新相关正式文档，避免每新增一个脚本就改多份文档。
- **按文档职责更新**：架构细节写 `03-技术架构文档.md`，任务状态和阶段日志写 `06-开发计划.md`，操作步骤写 `设计文档/操作指南/`；不要把细节重复写进 `CODEBUDDY.md`。
- **先文档后代码的适用范围**：仅限用户说明要求如此时，一般是在做玩法规则、架构方向、资源规范等大的设计变更时用户会需要使用；普通实现细节不需要先改文档。
- **会议内容 ≠ 正式文档**：`设计文档/会议内容/` 仅为原始记录，结论必须同步到正式文档才生效。
- **编辑器菜单规范**：所有编辑器工具放在 `BlockPuzzle/` 顶层菜单下，不用 `Tools/`。人工日常工具放 `BlockPuzzle/xxx`，AI 一次性生成工具放 `BlockPuzzle/AI 工具/xxx`。详见 `03-技术架构文档.md § 12`。


# CLAUDE.md — 12-rule template

These rules apply to every task in this project unless explicitly overridden.
Bias: caution over speed on non-trivial work. Use judgment on trivial tasks.

## Rule 1 — Think Before Coding
State assumptions explicitly. If uncertain, ask rather than guess.
Present multiple interpretations when ambiguity exists.
Push back when a simpler approach exists.
Stop when confused. Name what's unclear.

## Rule 2 — Simplicity First
Minimum code that solves the problem. Nothing speculative.
No features beyond what was asked. No abstractions for single-use code.
Test: would a senior engineer say this is overcomplicated? If yes, simplify.

## Rule 3 — Surgical Changes
Touch only what you must. Clean up only your own mess.
Don't "improve" adjacent code, comments, or formatting.
Don't refactor what isn't broken. Match existing style.

## Rule 4 — Goal-Driven Execution
Define success criteria. Loop until verified.
Don't follow steps. Define success and iterate.
Strong success criteria let you loop independently.

## Rule 5 — Use the model only for judgment calls
Use me for: classification, drafting, summarization, extraction.
Do NOT use me for: routing, retries, deterministic transforms.
If code can answer, code answers.

## Rule 6 — Token budgets are not advisory
Per-task: 4,000 tokens. Per-session: 30,000 tokens.
If approaching budget, summarize and start fresh.
Surface the breach. Do not silently overrun.

## Rule 7 — Surface conflicts, don't average them
If two patterns contradict, pick one (more recent / more tested).
Explain why. Flag the other for cleanup.
Don't blend conflicting patterns.

## Rule 8 — Read before you write
Before adding code, read exports, immediate callers, shared utilities.
"Looks orthogonal" is dangerous. If unsure why code is structured a way, ask.

## Rule 9 — Tests verify intent, not just behavior
Tests must encode WHY behavior matters, not just WHAT it does.
A test that can't fail when business logic changes is wrong.

## Rule 10 — Checkpoint after every significant step
Summarize what was done, what's verified, what's left.
Don't continue from a state you can't describe back.
If you lose track, stop and restate.

## Rule 11 — Match the codebase's conventions, even if you disagree
Conformance > taste inside the codebase.
If you genuinely think a convention is harmful, surface it. Don't fork silently.

## Rule 12 — Fail loud
"Completed" is wrong if anything was skipped silently.
"Tests pass" is wrong if any were skipped.
Default to surfacing uncertainty, not hiding it.