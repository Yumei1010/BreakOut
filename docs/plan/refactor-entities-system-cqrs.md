# BreakOut 阶段二重构蓝图：entities + system + cqrs 三层

> 状态：规划定稿（2026-09）| 目标：把复刻版的 Godot 视图耦合，重组为 Twenty-four 风格的三层组织，rules/ 独立保留纯 C# 可测层。

## 目标目录

```
scripts/
├── rules/               # ✅ 已完成（domain 迁入）：纯 C# 规则层，8 域，89 测试
├── entities/            # Godot 实体薄壳（partial 5 文件拆分，Twenty-four 模式）
│   ├── ball/            #   BallView 拆分
│   ├── paddle/          #   PaddleView + LaserView
│   ├── brick/           #   BrickView
│   ├── game/            #   GameView（原 GameRoot 拆分后的根实体）
│   └── ui/              #   HUD/弹层视图
├── system/              # 架构系统：订阅事件→调 rules→发事件/驱动实体
│   ├── run/             #   RunFlowSystem：发球/落底/死亡/过关状态流转
│   ├── scoring/         #   ScoringSystem：计分/连击/能量
│   ├── brick/           #   BrickSystem：砖墙注册表 + 连锁
│   ├── level/           #   LevelSystem：关卡生成
│   ├── bump/            #   BumpSystem：击球判定
│   ├── ability/         #   AbilitySystem：能力规则调度
│   ├── effect/          #   EffectSystem：粒子/音效/震动（纯表现协调）
│   └── ui/              #   UiOverlaySystem：GameOver/StageClear/Ultimate 弹层
├── cqrs/                # 命令（意图入口）/事件（跨层通知）
│   ├── run/event/       #   BallLost/EnergyChanged + command 意图
│   ├── scoring/event/
│   ├── brick/event/
│   ├── bump/event/
│   └── game/command/    #   发球/重开意图
└── framework/ core/ component/ ...  # 保留
```

## 现有文件 → 目标映射

| 现有文件 | 目标 | 动作 |
|---|---|---|
| presentation/game/GameRoot.cs | entities/game/GameView.cs + system/* | **拆分**（最大变更） |
| presentation/ball/BallView.cs | entities/ball/BallView.cs + partial | 搬移 + partial 化 |
| presentation/paddle/PaddleView.cs | entities/paddle/PaddleView.cs + partial | 搬移 + partial 化 |
| presentation/paddle/LaserView.cs | entities/paddle/LaserView.cs | 搬移 |
| presentation/brick/BrickView.cs | entities/brick/BrickView.cs + partial | 搬移 + partial 化 |
| presentation/ui/*.cs (HUD/弹层) | entities/ui/*.cs | 搬移 |
| presentation/assets/GameTextures.cs | framework/ 或保留 assets 工具 | 搬移 |
| presentation/assets/SfxManager.cs | system/effect/EffectSystem 下属 | 收编 |
| presentation/effect/CameraShake.cs | system/effect/CameraFx 下属 | 收编 |
| cqrs/*/event/ | 保留 + 补命令 | 扩充 |
| rules/* | 不变 ✅ | — |

## GameRoot 拆分映射（核心）

| GameRoot 现职责 | 迁入 |
|---|---|
| _Ready 组装/预加载 | GameView（根实体） |
| ResolveSceneNodes | GameView.Dependencies |
| GenerateLevel/ClearBricks | LevelSystem + BrickSystem |
| OnBallLost/PlayDeathSequence | RunFlowSystem（订阅/调用） |
| OnBrickHit/OnBrickDestroyed/OnBumpJudged | ScoringSystem + BrickSystem |
| ShowCombo/TryShowUltimate | ScoringSystem（事件驱动） |
| ShowGameOver/StageClear/Ultimate | UiOverlaySystem |
| Sfx/Shake/SpawnParticle/PatternBounce | EffectSystem |
| BuildStageResult（统计） | ScoringSystem |
| BurstDebris/FadeGrayscale/相机序列 | EffectSystem.CameraFx |

## 实体 partial 拆分约定（Twenty-four 模式）

每个实体（Ball/Paddle/Brick/Game）：
```
*.cs                # 核心：_Ready 只做 ReadyAsync→ConnectSignal→RegisterEvent + Godot 生命周期
*.Dependencies.cs   # GetNode 节点引用 + 场景组装
*.Properties.cs     # 字段/属性/配置
*.Events.cs         # RegisterEvent 订阅 cqrs 事件
*.Signals.cs        # Godot 信号 → cqrs 意图（命令/事件）
```

## 系统形态（关键设计）

- 系统实现 GFramework `ISystem`（注册进架构 SystemModule）
- 系统**订阅 cqrs 事件**（规则级变化）→ 调 rules 纯 C# → 发新事件
- 系统持有 rules 实例（RunState/ScoreRule/BrickField 归 system 持有，不再被视图直持）
- 每帧物理/juice 仍在实体（视图）直写，不事件化（纪律不变）

## 执行顺序（每步可独立验证）

| 步 | 内容 | 验证 |
|---|---|---|
| 1 ✅ | domain → rules/ | 89 测试 |
| 2 | 建 system/ 骨架 + GameRoot 拆出 LevelSystem/ScoringSystem（规则持有权移交） | build + headless + 89 测试 |
| 3 | RunFlowSystem + BrickSystem（状态流转/砖墙收编） | 完整玩法可跑 |
| 4 | EffectSystem + UiOverlaySystem（特效/弹层收编） | 特效回归 |
| 5 | 实体 partial 拆分 + cqrs 命令补齐 | 组织到位 |
| 6 | 收尾：命名空间清理/测试对齐/沉淀文档 | 全绿 |

## 验收

- [ ] scripts/entities|system|cqrs 三层组织清晰，rules/ 纯 C# 独立
- [ ] GameRoot 拆除（无 700+ 行上帝类）
- [ ] 89 测试全绿（rules 层不变）+ headless 完整可玩
- [ ] 视图不再直持 RunState/ScoreRule/BrickField（规则宿主=system）
- [ ] 实体按 5 partial 拆分
