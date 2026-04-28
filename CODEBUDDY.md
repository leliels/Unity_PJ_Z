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
| M2 数值体验+换皮 | 🔵 收尾中 | 任务1-4已完成，任务5 Prefab化已完成大部分（13个Prefab） |
| M3 完整玩法 | ⬜ | UI流程+游戏模式+存档+音效 |
| M4 内容填充 | ⬜ | 特效+音效+冒险模式机制 |
| M5 打磨上线 | ⬜ | Bug修复+适配+上架 |

## 技术栈

- Unity 6 (6000.3.8f1)，Universal 2D 模板，URP 17.3.0
- C#，New Input System 1.18.0，uGUI
- Git 版本控制，不依赖第三方插件（不用 DOTween）

## 关键技术决策

- 竖屏 1080×1920 | 8×8 棋盘 | 3 候选方块用完刷新
- 消除后不下落 | 不支持旋转 | 计分 16^N + Combo(+0.2/次)
- 数据存储 PlayerPrefs | 先做 PC 再移动端
- 棋盘代码动态生成（非 Tilemap）| M2 开始 Prefab 化

## 项目目录结构

```
Unity_PJ_Z/
├── CODEBUDDY.md              ← 你正在读的文件（项目索引）
├── README.md                  # Git 仓库首页（极简）
├── Assets/
│   ├── Scripts/               # C# 脚本（17 个，按模块分目录）
│   │   ├── Core/              # GameManager, SceneBootstrap, Singleton, GameState
│   │   ├── Board/             # BoardManager, MatchChecker
│   │   ├── Block/             # BlockData, BlockSpawner, BlockDrag
│   │   ├── Score/             # ScoreManager
│   │   ├── UI/                # GameUI, FloatingScoreManager, NumberImageDisplay
│   │   ├── Utils/             # Constants, SpriteUtils
│   │   └── Editor/            # CreateDigitAtlas, CreateGamePrefabs（编辑器工具）
│   ├── Scenes/Boot.unity      # 游戏主场景（挂 SceneBootstrap 启动一切）
│   ├── Resources/
│   │   ├── Art/               # 运行时加载的美术资源（Blocks/Board/Backgrounds/UI）
│   │   └── Digits/            # 数字精灵图（SH1/SH2 系列）
│   ├── Art/拆分资源/           # 美术原始切图（效果图 + UI 素材）
│   ├── Prefabs/               # 预制体
│   │   ├── Block/             # BlockCell
│   │   ├── Board/             # Cell, Preview, CandidateSlot, [BoardManager], [BlockSpawner]
│   │   └── UI/                # FloatingScore, GameOverPanel, ScoreDisplay, HighScoreDisplay 等
│   └── Settings/              # URP 渲染设置
├── Packages/                  # Unity 包管理
├── ProjectSettings/           # 项目设置
└── 设计文档/                   # 设计文档目录（见下方索引）
    └── 操作指南/               # 操作类使用说明文档
```

## 设计文档索引

> 详细内容请查阅对应文档，此处仅做索引。

| 文档 | 定位 | 何时需要读 |
|------|------|-----------|
| @设计文档/01-项目总览.md | 团队分工、技术环境、已安装的 Unity 包 | 了解团队和技术环境 |
| @设计文档/02-游戏设计文档.md | 核心玩法规则、消除机制、计分公式、关卡系统、游戏流程 | 实现或修改玩法逻辑 |
| @设计文档/03-技术架构文档.md | 脚本架构、目录规划、代码规范、Manager 系统设计 | 写代码前必读 |
| @设计文档/04-UI界面规划.md | 所有界面布局、元素清单 | 实现 UI 时 |
| @设计文档/05-美术资源规范.md | 命名规范、尺寸要求、素材清单、特效需求 | 制作或接入美术资源 |
| @设计文档/06-开发计划.md | 里程碑任务清单、当前待办、开发日志 | 了解进度或接续开发 |
| @设计文档/07-待确认问题清单.md | 已确认和未确认的设计决策 | 遇到不确定的设计问题 |
| @设计文档/08-美术资源替换指南.md | Resources 目录结构、资源映射表、替换流程 | 替换或接入美术资源 |

**操作指南**（使用说明类文档，非设计文档）：

| 文档 | 说明 |
|------|------|
| @设计文档/操作指南/SpriteAtlas图集打包指南.md | 数字图集的生成和使用方式 |

## 架构要点

- **SceneBootstrap**（Boot 场景唯一挂载脚本）负责启动一切：创建 Manager、创建 UI、配置相机
- **Manager 单例模式**：GameManager → BoardManager / BlockSpawner / ScoreManager / GameUI
- **通信方式**：C# 原生 event/Action，不用消息总线
- **Prefab 化已大量完成**（M2 任务5）：棋盘格子、方块、候选区、得分飘字、分数显示等均已有 Prefab，可在 Inspector 调整参数
- 代码中保留 fallback（Prefab/资源为 null 时走代码生成路径，确保不崩溃）

## 开发规则

- **先文档后代码**：大的功能设计变更，先更新设计文档，再写代码
- **保持文档同步**：代码完成后更新 06-开发计划.md 的 checkbox 和开发日志
- **会议内容 ≠ 正式文档**：`设计文档/会议内容/` 仅为原始记录，结论必须同步到正式文档才生效
